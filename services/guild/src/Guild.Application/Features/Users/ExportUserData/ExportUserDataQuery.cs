using Guild.Application.Abstractions.Messaging;
using Guild.Application.Abstractions.Persistence;
using Guild.Domain.Results;

namespace Guild.Application.Features.Users.ExportUserData;

public sealed record ExportUserDataQuery(long UserId) : IQuery<Result<UserDataExportResponse>>;

/// <summary>
/// the user's Guild-owned data for a GDPR self-serve export: guilds they own and
/// the guilds they belong to (with nickname and role names). intentionally scoped
/// to intelligible, subject-centric data - no bans (moderation records) or
/// permission overwrites (internal config).
/// </summary>
public sealed record UserDataExportResponse(
	string UserId,
	IReadOnlyList<OwnedGuildDto> OwnedGuilds,
	IReadOnlyList<MembershipDto> Memberships);

public sealed record OwnedGuildDto(string Name, DateTimeOffset CreatedAt)
{
	public static OwnedGuildDto From(ExportedGuild g) => new(g.Name, g.CreatedAt);
}

public sealed record MembershipDto(
	string GuildName,
	string? Nickname,
	DateTimeOffset JoinedAt,
	IReadOnlyList<string> Roles)
{
	public static MembershipDto From(ExportedMembership m) => new(m.GuildName, m.Nickname, m.JoinedAt, m.Roles);
}
