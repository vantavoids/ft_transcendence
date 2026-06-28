using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Security;
using Guild.Application.Authorization;
using Guild.Domain.Guild;
using Guild.Domain.Results;

namespace Guild.Application.Features.Bans.UnbanMember;

internal sealed class UnbanMemberHandler(
	IGuildRepository guilds,
	IGuildBanRepository bans,
	ICurrentUser currentUser,
	IUnitOfWork unitOfWork)
	: ICommandHandler<UnbanMemberCommand, Result>
{
	public async Task<Result> HandleAsync(
		UnbanMemberCommand command,
		CancellationToken cancellationToken = default)
	{
		var auth = await AuthorizationContext.LoadAsync(
			guilds, currentUser, command.GuildId, Permission.BanMembers, cancellationToken);
		if (auth.IsFailure)
			return auth.Error;
		var guild = auth.Value.Guild;

		var ban = await bans.FindAsync(guild.Id, command.TargetUserId, cancellationToken);
		if (ban is null)
			return GuildFailures.BanNotFound;

		bans.Remove(ban);
		await unitOfWork.SaveChangesAsync(cancellationToken);

		return Result.Ok();
	}
}
