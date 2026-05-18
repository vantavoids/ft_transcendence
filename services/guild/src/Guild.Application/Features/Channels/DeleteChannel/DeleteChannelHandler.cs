using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Channels.DeleteChannel;

internal sealed class DeleteChannelHandler(
	IGuildRepository guilds,
	IChannelRepository channels,
	ICurrentUser currentUser)
	: ICommandHandler<DeleteChannelCommand, Result>
{
	public async Task<Result> HandleAsync(
		DeleteChannelCommand command,
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

		channels.Remove(channel);
		await channels.SaveChangesAsync(cancellationToken);

		return Result.Ok();
	}
}
