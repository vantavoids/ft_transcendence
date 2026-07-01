using Guild.Application.Abstractions.Persistence;

namespace Guild.Application.Features.Guilds.ListMyGuilds;

/// <summary>
/// wire shape for one entry of <c>GET /guilds/me</c>. snowflake IDs are emitted
/// as quoted strings so JS clients can hold them without precision loss.
/// <c>member_count</c> is the guild's current size; <c>joined_at</c> is the
/// caller's own membership timestamp
/// </summary>
public sealed record MyGuildDto(
	string Id,
	string Name,
	string? IconUrl,
	string OwnerId,
	int MemberCount,
	DateTimeOffset JoinedAt)
{
	public static MyGuildDto From(MyGuildSummary summary) => new(
		Id: summary.Id.ToString(),
		Name: summary.Name,
		IconUrl: summary.IconUrl,
		OwnerId: summary.OwnerId.ToString(),
		MemberCount: summary.MemberCount,
		JoinedAt: summary.JoinedAt);
}
