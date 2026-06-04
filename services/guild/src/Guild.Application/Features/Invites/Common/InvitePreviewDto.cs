namespace Guild.Application.Features.Invites.Common;

public sealed record InvitePreviewGuildDto(
	string Id,
	string Name,
	string? IconUrl,
	int MemberCount);

public sealed record InvitePreviewInviterDto(
	string Id,
	string Username);

public sealed record InvitePreviewDto(
	string Code,
	InvitePreviewGuildDto Guild,
	InvitePreviewInviterDto Inviter,
	DateTimeOffset? ExpiresAt);
