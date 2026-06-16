using Guild.Domain.Guild;

namespace Guild.Application.Features.Bans.Common;

/// <summary>
/// wire shape for a guild ban. ids are snowflakes serialised as strings so
/// JS clients hold them without precision loss.
/// </summary>
public sealed record BanResponse(
	string UserId,
	string BannedBy,
	string? Reason,
	DateTimeOffset BannedAt)
{
	public static BanResponse From(GuildBan ban) => new(
		UserId: ban.UserId.ToString(),
		BannedBy: ban.BannedBy.ToString(),
		Reason: ban.Reason,
		BannedAt: ban.BannedAt);
}
