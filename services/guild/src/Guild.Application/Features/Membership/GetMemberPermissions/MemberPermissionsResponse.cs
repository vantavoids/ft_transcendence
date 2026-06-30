namespace Guild.Application.Features.Membership.GetMemberPermissions;

/// <summary>
/// guild-scoped effective permissions for a member. channel overwrites are NOT
/// folded in here (use the internal channel-membership endpoint for that).
/// for the owner, <c>EffectivePermissions</c> is the Administrator bit only,
/// matching <see cref="Domain.Guild.PermissionResolver.Resolve(long, long, IEnumerable{Domain.Guild.Role}, IEnumerable{Domain.Guild.MemberRole})"/>;
/// the <c>IsOwner</c> flag is the canonical "wins everything" signal.
/// </summary>
public sealed record MemberPermissionsResponse(
	string UserId,
	string GuildId,
	IReadOnlyList<MemberRoleSnapshot> Roles,
	long EffectivePermissions,
	bool IsOwner);

public sealed record MemberRoleSnapshot(string Id, string Name, long Permissions);
