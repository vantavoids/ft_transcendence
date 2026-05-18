using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Features.Channels.Common;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Channels.UpdateChannel;

internal sealed class UpdateChannelHandler(
	IGuildRepository guilds,
	IChannelRepository channels,
	IChannelCategoryRepository categories,
	IClock clock,
	ICurrentUser currentUser)
	: ICommandHandler<UpdateChannelCommand, Result<ChannelResponse>>
{
	public async Task<Result<ChannelResponse>> HandleAsync(
		UpdateChannelCommand command,
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

		var channel = await channels.GetByIdAsync(command.ChannelId, cancellationToken);
		if (channel is null || channel.GuildId != command.GuildId)
			return GuildFailures.ChannelNotFound;

		var now = clock.UtcNow;

		if (command.Name is not null)
		{
			var renameResult = channel.Rename(command.Name, now);
			if (renameResult.IsFailure)
				return renameResult.Error;
		}

		if (command.TopicProvided)
		{
			var topicResult = channel.UpdateTopic(command.Topic, now);
			if (topicResult.IsFailure)
				return topicResult.Error;
		}

		if (command.Position is { } newPosition)
		{
			var repositionResult = channel.Reposition(newPosition, now);
			if (repositionResult.IsFailure)
				return repositionResult.Error;
		}

		if (command.CategoryIdProvided)
		{
			if (command.CategoryId is { } newCategoryId)
			{
				var newCategory = await categories.GetByIdAsync(command.GuildId, newCategoryId, cancellationToken);
				if (newCategory is null)
					return GuildFailures.CategoryNotFound;
			}

			var moveResult = channel.MoveToCategory(command.CategoryId, now);
			if (moveResult.IsFailure)
				return moveResult.Error;
		}

		channels.Update(channel);
		await channels.SaveChangesAsync(cancellationToken);

		return ChannelResponse.From(channel);
	}
}
