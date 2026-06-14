using Guild.Domain.Guild;

namespace Guild.Application.Features.Roles.Common;

/// <summary>
/// wire shape for a Role. snowflake ids are quoted strings so JS clients hold
/// them without precision loss. <c>permissions</c> is the raw bitmask as a
/// number (long) since clients typically render via bitwise ANDs against the
/// well-known Permission constants
/// </summary>
public sealed record RoleResponse(
	string Id,
	string GuildId,
	string Name,
	string? Color,
	long Permissions,
	int Position,
	bool IsHoisted,
	bool IsMentionable,
	bool IsDefault)
{
	public static RoleResponse From(Role role) => new(
		Id: role.Id.ToString(),
		GuildId: role.GuildId.ToString(),
		Name: role.Name,
		Color: role.Color,
		Permissions: role.Permissions,
		Position: role.Position,
		IsHoisted: role.IsHoisted,
		IsMentionable: role.IsMentionable,
		IsDefault: role.IsDefault);
}
