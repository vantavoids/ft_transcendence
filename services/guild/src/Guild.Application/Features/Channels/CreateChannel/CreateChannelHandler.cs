using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
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
	ICurrentUser currentUser)
	: ICommandHandler<CreateChannelCommand, Result<ChannelResponse>>
{
	public async Task<Result<ChannelResponse>> HandleAsync(
		CreateChannelCommand command,
		CancellationToken cancellationToken = default)
	{
		var guild = await guilds.GetByIdWithMembershipAsync(command.GuildId, cancellationToken);
		if (guild is null)
			return GuildFailures.GuildNotFound;

		if (guild.Members.All(m => m.UserId != currentUser.Id))
			return GuildFailures.NotAMember;

		var mask = PermissionResolver.Resolve(
			currentUser.Id, guild.OwnerId, guild.Roles, guild.MemberRoles);
		if (!PermissionResolver.HasPermission(mask, Permission.ManageChannels))
			return GuildFailures.MissingPermission;

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
			now: clock.UtcNow);

		if (channelResult.IsFailure)
			return channelResult.Error;

		await channels.AddAsync(channelResult.Value, cancellationToken);
		await channels.SaveChangesAsync(cancellationToken);

		return ChannelResponse.From(channelResult.Value);
	}

	private static bool TryParseType(string? raw, out ChannelType type)
	{
		// accept lowercase "text"/"voice" per contract; tolerate case-insensitive
		switch (raw?.Trim().ToLowerInvariant())
		{
			case null:
			case "":
			case "text":
				type = ChannelType.Text;
				return true;
			case "voice":
				type = ChannelType.Voice;
				return true;
			default:
				type = default;
				return false;
		}
	}
}
