using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Authorization;
using Guild.Application.Features.Channels.Common;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Channels.CreateChannel;

internal sealed class CreateChannelHandler(
	IGuildRepository guilds,
	IChannelRepository channels,
	IChannelCategoryRepository categories,
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

		channels.Add(channelResult.Value);
		await unitOfWork.SaveChangesAsync(cancellationToken);

		return ChannelResponse.From(channelResult.Value);
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
}
