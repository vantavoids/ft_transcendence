using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Authorization;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Channels.Permissions.PutOverwrite;

internal sealed class PutOverwriteHandler(
	IGuildRepository guilds,
	IChannelRepository channels,
	IChannelPermissionOverwriteRepository overwrites,
	IEventBus eventBus,
	IIdGenerator ids,
	IClock clock,
	ICurrentUser currentUser,
	IUnitOfWork unitOfWork)
	: ICommandHandler<PutOverwriteCommand, Result>
{
	public async Task<Result> HandleAsync(
		PutOverwriteCommand command,
		CancellationToken cancellationToken = default)
	{
		var channel = await channels.GetByIdAsync(command.ChannelId, cancellationToken);
		if (channel is null)
			return GuildFailures.ChannelNotFound;

		var auth = await AuthorizationContext.LoadAsync(
			guilds, currentUser, channel.GuildId, Permission.ManageChannels, cancellationToken);
		if (auth.IsFailure)
			return auth.Error;
		var guild = auth.Value.Guild;

		if (!TryParseTargetType(command.TargetType, out var targetType))
			return GuildFailures.OverwriteInvalidTarget;

		// target_id must reference a real role or member of *this* guild
		if (targetType == OverwriteTargetType.Role)
		{
			if (guild.Roles.All(r => r.Id != command.TargetId))
				return GuildFailures.OverwriteInvalidTarget;
		}
		else
		{
			if (guild.Members.All(m => m.UserId != command.TargetId))
				return GuildFailures.OverwriteInvalidTarget;
		}

		var channelOverwrites = await overwrites.GetForChannelAsync(channel.Id, cancellationToken);
		var existing = channelOverwrites.FirstOrDefault(
			o => o.TargetType == targetType && o.TargetId == command.TargetId);

		var snapshot = ChannelAccess.CaptureForChannel(
			guild, channel.Id,
			ChannelAccess.MembersAffectedBy(guild, targetType, command.TargetId),
			channelOverwrites);

		var now = clock.UtcNow;
		IReadOnlyList<ChannelPermissionOverwrite> after;

		if (existing is not null)
		{
			var updateResult = existing.UpdatePermissions(command.Allow, command.Deny, now);
			if (updateResult.IsFailure)
				return updateResult.Error;

			overwrites.Update(existing);
			after = channelOverwrites; // existing is the same tracked instance, now updated in place
		}
		else
		{
			var createResult = ChannelPermissionOverwrite.Create(
				id: ids.NextId(),
				guildId: channel.GuildId,
				channelId: channel.Id,
				targetType: targetType,
				targetId: command.TargetId,
				allow: command.Allow,
				deny: command.Deny,
				now: now);

			if (createResult.IsFailure)
				return createResult.Error;

			overwrites.Add(createResult.Value);
			after = [.. channelOverwrites, createResult.Value];
		}

		await ChannelAccess.PublishAccessChangesAsync(eventBus, guild, snapshot, channel.Id, after, cancellationToken);

		await unitOfWork.SaveChangesAsync(cancellationToken);

		return Result.Ok();
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
