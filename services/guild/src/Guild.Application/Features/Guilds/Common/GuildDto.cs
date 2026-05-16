using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.Application.Features.Guilds.Common;

/// <summary>
/// wire shape for a Guild. snowflake IDs are converted to quoted strings at the
/// DTO boundary so JS clients can hold them without precision loss
/// </summary>
public sealed record GuildDto(
	string Id,
	string Name,
	string? Description,
	string? IconUrl,
	string? BannerUrl,
	string OwnerId,
	DateTimeOffset CreatedAt)
{
	public static GuildDto FromEntity(GuildEntity guild) => new(
		Id: guild.Id.ToString(),
		Name: guild.Name,
		Description: guild.Description,
		IconUrl: guild.IconUrl,
		BannerUrl: guild.BannerUrl,
		OwnerId: guild.OwnerId.ToString(),
		CreatedAt: guild.CreatedAt);
}

/// <summary>variant returned by <c>GET /guilds/{id}</c> with the member count attached</summary>
public sealed record GuildDetailsDto(
	string Id,
	string Name,
	string? Description,
	string? IconUrl,
	string? BannerUrl,
	string OwnerId,
	int MemberCount,
	DateTimeOffset CreatedAt)
{
	public static GuildDetailsDto FromEntity(GuildEntity guild, int memberCount) => new(
		Id: guild.Id.ToString(),
		Name: guild.Name,
		Description: guild.Description,
		IconUrl: guild.IconUrl,
		BannerUrl: guild.BannerUrl,
		OwnerId: guild.OwnerId.ToString(),
		MemberCount: memberCount,
		CreatedAt: guild.CreatedAt);
}
