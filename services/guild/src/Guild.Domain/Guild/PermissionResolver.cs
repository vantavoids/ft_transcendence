namespace Guild.Domain.Guild;

/// <summary>
/// pure helper that computes the effective permission bitmask for a user in a Guild
/// the owner short-circuits to <see cref="Permission.Administrator"/>;
/// the ADMINISTRATOR bit short-circuits all permission checks.
/// </summary>
public static class PermissionResolver
{
	public static long Resolve(
		long userId,
		long ownerId,
		IEnumerable<Role> allGuildRoles,
		IEnumerable<MemberRole> userAssignments)
	{
		if (userId == ownerId)
			return (long)Permission.Administrator;

		long mask = 0L;

		// the default (@everyone) role is always granted, regardless of explicit
		// assignment. it encodes the baseline permissions for all members
		foreach (var role in allGuildRoles)
		{
			if (role.IsDefault)
				mask |= role.Permissions;
		}

		// explicitly assigned roles add to the mask
		var assignedRoleIds = new HashSet<long>();
		foreach (var assignment in userAssignments)
		{
			if (assignment.UserId == userId)
				assignedRoleIds.Add(assignment.RoleId);
		}

		if (assignedRoleIds.Count > 0)
		{
			foreach (var role in allGuildRoles)
			{
				if (assignedRoleIds.Contains(role.Id))
					mask |= role.Permissions;
			}
		}

		return mask;
	}

	/// <summary>
	/// returns true if <paramref name="effectiveMask"/> grants <paramref name="required"/>,
	/// either explicitly or implicitly via the <see cref="Permission.Administrator"/> bit
	/// </summary>
	public static bool HasPermission(long effectiveMask, Permission required)
	{
		if ((effectiveMask & (long)Permission.Administrator) != 0)
			return true;
		return (effectiveMask & (long)required) == (long)required;
	}
}
