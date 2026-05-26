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

		// the default (@everyone) role is always granted, regardless of explicit
		// assignment. it encodes the baseline permissions for all members
		var mask = allGuildRoles.Where(role => role.IsDefault).Aggregate(0L, (current, role) => current | role.Permissions);

		// explicitly assigned roles add to the mask
		var assignedRoleIds = new HashSet<long>();
		foreach (var assignment in userAssignments)
		{
			if (assignment.UserId == userId)
				assignedRoleIds.Add(assignment.RoleId);
		}

		return assignedRoleIds.Count <= 0 ? mask : allGuildRoles.Where(role => assignedRoleIds.Contains(role.Id)).Aggregate(mask, (current, role) => current | role.Permissions);
	}

	/// <summary>
	/// channel-scoped resolution. follows Discord's precedence order:
	/// see https://discord.com/developers/docs/topics/permissions#permission-overwrites
	///
	/// 1. owner -> all bits
	/// 2. non-member -> 0
	/// 3. start from role-aggregate (incl. @everyone)
	/// 4. ADMINISTRATOR -> all bits
	/// 5. apply @everyone channel overwrite
	/// 6. apply aggregate of all other role overwrites for roles the member holds
	/// 7. apply member-targeted channel overwrite (highest precedence)
	/// </summary>
	public static long Resolve(
		Guild guild,
		long userId,
		IReadOnlyList<ChannelPermissionOverwrite> overwrites)
	{
		if (userId == guild.OwnerId)
			return ~0L;

		// must be a real member to have any permissions on a channel of that guild
		if (guild.Members.All(m => m.UserId != userId))
			return 0L;

		// build role-aggregate from @everyone + assigned roles
		var assignedRoleIds = guild.MemberRoles
			.Where(mr => mr.UserId == userId)
			.Select(mr => mr.RoleId)
			.ToHashSet();

		var perms = 0L;
		Role? everyone = null;
		foreach (var role in guild.Roles)
		{
			if (role.IsDefault)
			{
				everyone = role;
				perms |= role.Permissions;
				continue;
			}

			if (assignedRoleIds.Contains(role.Id))
				perms |= role.Permissions;
		}

		if ((perms & (long)Permission.Administrator) != 0L)
			return ~0L;

		// step 5: @everyone overwrite
		if (everyone is not null)
		{
			var everyoneOw = overwrites.FirstOrDefault(o =>
				o.TargetType == OverwriteTargetType.Role && o.TargetId == everyone.Id);
			if (everyoneOw is not null)
				perms = (perms & ~everyoneOw.Deny) | everyoneOw.Allow;
		}

		// step 6: aggregate of other role overwrites the member has
		var aggAllow = 0L;
		var aggDeny = 0L;
		foreach (var ow in overwrites)
		{
			if (ow.TargetType != OverwriteTargetType.Role)
				continue;
			if (everyone is not null && ow.TargetId == everyone.Id)
				continue;
			if (!assignedRoleIds.Contains(ow.TargetId))
				continue;

			aggAllow |= ow.Allow;
			aggDeny |= ow.Deny;
		}
		perms = (perms & ~aggDeny) | aggAllow;

		// step 7: member overwrite has the final word
		var memberOw = overwrites.FirstOrDefault(o =>
			o.TargetType == OverwriteTargetType.Member && o.TargetId == userId);
		if (memberOw is not null)
			perms = (perms & ~memberOw.Deny) | memberOw.Allow;

		return perms;
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
