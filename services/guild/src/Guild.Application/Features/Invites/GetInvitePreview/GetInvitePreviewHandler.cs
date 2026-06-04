using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Application.Abstractions.Users;
using Guild.Application.Features.Invites.Common;
using Guild.Domain.Results;

namespace Guild.Application.Features.Invites.GetInvitePreview;

internal sealed class GetInvitePreviewHandler(
	IGuildRepository guilds,
	IGuildInviteRepository invites,
	IUserService users,
	IClock clock)
	: IQueryHandler<GetInvitePreviewQuery, Result<InvitePreviewDto>>
{
	public async Task<Result<InvitePreviewDto>> HandleAsync(
		GetInvitePreviewQuery query,
		CancellationToken cancellationToken = default)
	{
		var invite = await invites.GetByCodeAsync(query.Code, cancellationToken);
		if (invite is null || !invite.IsActive(clock.UtcNow))
			return GuildFailures.InviteNotFound;

		var guild = await guilds.GetByIdAsync(invite.GuildId, cancellationToken);
		if (guild is null)
			return GuildFailures.InviteNotFound;

		var memberCount = await guilds.CountMembersAsync(invite.GuildId, cancellationToken);

		// inviter lookup is best-effort: if User Service is down or doesn't know
		// the inviter, fall back to an empty username so the preview still renders
		var inviter = await users.GetSummaryAsync(invite.CreatedBy, cancellationToken);
		var inviterDto = new InvitePreviewInviterDto(
			Id: invite.CreatedBy.ToString(),
			Username: inviter?.Username ?? string.Empty);

		return new InvitePreviewDto(
			Code: invite.Code,
			Guild: new InvitePreviewGuildDto(
				Id: guild.Id.ToString(),
				Name: guild.Name,
				IconUrl: guild.IconUrl,
				MemberCount: memberCount),
			Inviter: inviterDto,
			ExpiresAt: invite.ExpiresAt);
	}
}
