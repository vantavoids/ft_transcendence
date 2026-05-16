using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Features.Guilds.Common;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Guilds.UpdateGuild;

internal sealed class UpdateGuildHandler(
	IGuildRepository repository,
	IClock clock,
	ICurrentUser currentUser)
	: ICommandHandler<UpdateGuildCommand, Result<GuildDto>>
{
	public async Task<Result<GuildDto>> HandleAsync(
		UpdateGuildCommand command,
		CancellationToken cancellationToken = default)
	{
		var guild = await repository.GetByIdWithMembershipAsync(command.GuildId, cancellationToken);
		if (guild is null)
			return GuildFailures.GuildNotFound;

		var effectiveMask = PermissionResolver.Resolve(
			currentUser.Id,
			guild.OwnerId,
			guild.Roles,
			guild.MemberRoles);

		// MANAGE_GUILD also requires the caller to actually be a member; the
		// resolver gives @everyone perms unconditionally, but a non-member can
		// never legitimately reach this branch with MANAGE_GUILD.
		var isMember = guild.Members.Any(m => m.UserId == currentUser.Id);
		if (!isMember)
			return GuildFailures.NotAMember;

		if (!PermissionResolver.HasPermission(effectiveMask, Permission.ManageGuild))
			return GuildFailures.MissingPermission;

		var updateResult = guild.UpdateSettings(
			name: command.Name,
			description: command.Description,
			iconUrl: command.IconUrl,
			bannerUrl: command.BannerUrl,
			now: clock.UtcNow);

		if (updateResult.IsFailure)
			return updateResult.Error;

		repository.Update(guild);
		await repository.SaveChangesAsync(cancellationToken);

		return GuildDto.FromEntity(guild);
	}
}
