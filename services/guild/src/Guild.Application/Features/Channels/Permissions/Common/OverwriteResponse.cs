using Guild.Domain.Guild;

namespace Guild.Application.Features.Channels.Permissions.Common;

/// <summary>
/// wire shape for a <see cref="ChannelPermissionOverwrite"/>. snowflake IDs as
/// quoted strings; target_type is "role" or "member"
/// </summary>
public sealed record OverwriteResponse(
	string Id,
	string ChannelId,
	string TargetType,
	string TargetId,
	long Allow,
	long Deny)
{
	public static OverwriteResponse From(ChannelPermissionOverwrite o) => new(
		Id: o.Id.ToString(),
		ChannelId: o.ChannelId.ToString(),
		TargetType: o.TargetType == OverwriteTargetType.Role ? "role" : "user",
		TargetId: o.TargetId.ToString(),
		Allow: o.Allow,
		Deny: o.Deny);
}
