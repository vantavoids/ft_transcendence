namespace Guild.Application.Features.Membership.Common;

/// <summary>
/// wire shape for a guild member. ids are snowflakes serialised as strings so
/// JS clients hold them without precision loss. <c>Roles</c> only lists explicit
/// assignments from <c>member_roles</c>; the implicit <c>@everyone</c> is not
/// included
/// </summary>
public sealed record MemberResponse(
	string UserId,
	string GuildId,
	string? Nickname,
	IReadOnlyList<string> Roles,
	DateTimeOffset JoinedAt);
