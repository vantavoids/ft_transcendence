using Guild.Domain.Guild;

namespace Guild.Application.Features.Invites.Common;

public sealed record InviteDto(
	string Code,
	string GuildId,
	string CreatedBy,
	int? MaxUses,
	int Uses,
	DateTimeOffset? ExpiresAt,
	DateTimeOffset CreatedAt)
{
	public static InviteDto FromEntity(GuildInvite invite) => new(
		Code: invite.Code,
		GuildId: invite.GuildId.ToString(),
		CreatedBy: invite.CreatedBy.ToString(),
		MaxUses: invite.MaxUses,
		Uses: invite.Uses,
		ExpiresAt: invite.ExpiresAt,
		CreatedAt: invite.CreatedAt);
}
