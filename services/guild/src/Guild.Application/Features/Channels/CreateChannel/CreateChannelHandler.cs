using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Authorization;
using Guild.Application.Contracts;
using Guild.Application.Features.Channels.Common;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Channels.CreateChannel;

internal sealed class CreateChannelHandler(
	IGuildRepository guilds,
	IChannelRepository channels,
	IChannelCategoryRepository categories,
	IChannelPermissionOverwriteRepository overwrites,
	IEventBus eventBus,
	IIdGenerator ids,
	IClock clock,
	ICurrentUser currentUser,
	IUnitOfWork unitOfWork)
	: ICommandHandler<CreateChannelCommand, Result<ChannelResponse>>
{
	public async Task<Result<ChannelResponse>> HandleAsync(
		CreateChannelCommand command,
		CancellationToken cancellationToken = default)
	{
		var auth = await AuthorizationContext.LoadAsync(
			guilds, currentUser, command.GuildId, Permission.ManageChannels, cancellationToken);
		if (auth.IsFailure)
			return auth.Error;
		var guild = auth.Value.Guild;

		// optional category must belong to this guild if supplied
		if (command.CategoryId is { } categoryId)
		{
			var category = await categories.GetByIdAsync(command.GuildId, categoryId, cancellationToken);
			if (category is null)
				return GuildFailures.CategoryNotFound;
		}

		if (!TryParseType(command.Type, out var type))
			return GuildFailures.ChannelInvalidType;

		int position;
		if (command.Position is { } requested)
		{
			position = requested;
		}
		else
		{
			var max = await channels.GetMaxPositionAsync(command.GuildId, command.CategoryId, cancellationToken);
			position = (max ?? -1) + 1;
		}

		var channelResult = Channel.Create(
			id: ids.NextId(),
			guildId: command.GuildId,
			categoryId: command.CategoryId,
			name: command.Name,
			topic: command.Topic,
			type: type,
			position: position,
			now: clock.UtcNow,
			isNsfw: command.IsNsfw ?? false,
			slowmodeSeconds: command.SlowmodeSeconds ?? 0);

		if (channelResult.IsFailure)
			return channelResult.Error;

		var channel = channelResult.Value;

		// build any requested overwrites atomically so the channel is created
		// already carrying its intended permissions -- there is no window where it
		// is world-readable, and the GuildChannelCreated event below targets only
		// members who can actually read it.
		var overwritesResult = BuildOverwrites(guild, channel.Id, command.Overwrites, clock.UtcNow);
		if (overwritesResult.IsFailure)
			return overwritesResult.Error;
		var createdOverwrites = overwritesResult.Value;

		channels.Add(channel);
		foreach (var overwrite in createdOverwrites)
			overwrites.Add(overwrite);

		// publish before SaveChanges so the outbox binds the event to the same
		// transaction as the inserts.
		var eligible = ChannelAccess.ReadersOf(guild, channel.Id, createdOverwrites);
		await eventBus.PublishAsync(
			new GuildChannelCreated(command.GuildId, ChannelPayload.From(channel), eligible),
			cancellationToken);

		await unitOfWork.SaveChangesAsync(cancellationToken);

		return ChannelResponse.From(channel);
	}

	private Result<IReadOnlyList<ChannelPermissionOverwrite>> BuildOverwrites(
		Domain.Guild.Guild guild,
		long channelId,
		IReadOnlyList<ChannelOverwriteInput>? inputs,
		DateTimeOffset now)
	{
		if (inputs is not { Count: > 0 })
			return Result.Ok<IReadOnlyList<ChannelPermissionOverwrite>>([]);

		var built = new List<ChannelPermissionOverwrite>(inputs.Count);
		var seen = new HashSet<(OverwriteTargetType, long)>();

		foreach (var input in inputs)
		{
			if (!TryParseTargetType(input.TargetType, out var targetType))
				return GuildFailures.OverwriteInvalidTarget;

			// target_id must reference a real role or member of *this* guild
			var targetExists = targetType == OverwriteTargetType.Role
				? guild.Roles.Any(r => r.Id == input.TargetId)
				: guild.Members.Any(m => m.UserId == input.TargetId);
			if (!targetExists)
				return GuildFailures.OverwriteInvalidTarget;

			// a target may only be listed once; a duplicate is a client error
			if (!seen.Add((targetType, input.TargetId)))
				return GuildFailures.OverwriteInvalidTarget;

			var overwriteResult = ChannelPermissionOverwrite.Create(
				id: ids.NextId(),
				guildId: guild.Id,
				channelId: channelId,
				targetType: targetType,
				targetId: input.TargetId,
				allow: input.Allow,
				deny: input.Deny,
				now: now);

			if (overwriteResult.IsFailure)
				return overwriteResult.Error;

			built.Add(overwriteResult.Value);
		}

		return Result.Ok<IReadOnlyList<ChannelPermissionOverwrite>>(built);
	}

	private static bool TryParseType(string? raw, out ChannelType type)
	{
		// accept lowercase "text"/"announcement"/"voice" per contract; tolerate case-insensitive
		switch (raw?.Trim().ToLowerInvariant())
		{
			case null:
			case "":
			case "text":
				type = ChannelType.Text;
				return true;
			case "announcement":
				type = ChannelType.Announcement;
				return true;
			default:
				type = default;
				return false;
		}
	}

	private static bool TryParseTargetType(string? raw, out OverwriteTargetType type)
	{
		switch (raw?.Trim().ToLowerInvariant())
		{
			case "role":
				type = OverwriteTargetType.Role;
				return true;
			case "member":
			case "user":
				type = OverwriteTargetType.Member;
				return true;
			default:
				type = default;
				return false;
		}
	}
}
