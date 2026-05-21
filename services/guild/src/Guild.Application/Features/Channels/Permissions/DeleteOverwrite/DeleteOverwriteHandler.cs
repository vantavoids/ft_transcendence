using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Channels.Permissions.DeleteOverwrite;

internal sealed class DeleteOverwriteHandler(
	IGuildRepository guilds,
	IChannelRepository channels,
	IChannelPermissionOverwriteRepository overwrites,
	ICurrentUser currentUser)
	: ICommandHandler<DeleteOverwriteCommand, Result>
{
	public async Task<Result> HandleAsync(
		DeleteOverwriteCommand command,
		CancellationToken cancellationToken = default)
	{
		var channel = await channels.GetByIdAsync(command.ChannelId, cancellationToken);
		if (channel is null)
			return GuildFailures.ChannelNotFound;

		var guild = await guilds.GetByIdWithMembershipAsync(channel.GuildId, cancellationToken);
		if (guild is null)
			return GuildFailures.GuildNotFound;

		if (guild.Members.All(m => m.UserId != currentUser.Id))
			return GuildFailures.NotAMember;

		var mask = PermissionResolver.Resolve(
			currentUser.Id, guild.OwnerId, guild.Roles, guild.MemberRoles);
		if (!PermissionResolver.HasPermission(mask, Permission.ManageChannels))
			return GuildFailures.MissingPermission;

		// the URL carries only (channel_id, target_id); since snowflake ids are
		// unique across roles and members, scan both buckets and remove the one
		// that exists. if none exists, surface a 404
		var all = await overwrites.GetForChannelAsync(channel.Id, cancellationToken);
		var match = all.FirstOrDefault(o => o.TargetId == command.TargetId);
		if (match is null)
			return GuildFailures.OverwriteNotFound;

		overwrites.Remove(match);
		await overwrites.SaveChangesAsync(cancellationToken);

		return Result.Ok();
	}
}
