import type { GuildRoleDto } from '../api/guild';

// Client-side mirror of the guild service's PermissionResolver, used to gate
// the member-role UI so users don't see actions the server would 403. The
// server remains the source of truth for every mutation.
export const PERMISSIONS = {
  ManageRoles: 64,
  Administrator: 256
} as const;

export function rolePermissionBits(role: GuildRoleDto): number {
  return Number(role.permissions) || 0;
}

export function hasPermission(mask: number, bits: number): boolean {
  if ((mask & PERMISSIONS.Administrator) !== 0) {
    return true;
  }

  return (mask & bits) === bits;
}

// Union of the @everyone role (from allRoles) and the member's assigned roles.
// The owner resolves to Administrator, which grants everything.
export function effectivePermissions(
  assignedRoles: GuildRoleDto[],
  allRoles: GuildRoleDto[],
  isOwner: boolean
): number {
  if (isOwner) {
    return PERMISSIONS.Administrator;
  }

  let mask = 0;

  for (const role of allRoles) {
    if (role.is_default) {
      mask |= rolePermissionBits(role);
    }
  }

  for (const role of assignedRoles) {
    mask |= rolePermissionBits(role);
  }

  return mask;
}

// Highest position among explicitly-assigned non-default roles; the owner
// outranks everyone and a member with no roles outranks no one.
export function memberRank(assignedRoles: GuildRoleDto[], isOwner: boolean): number {
  if (isOwner) {
    return Infinity;
  }

  let rank = -Infinity;

  for (const role of assignedRoles) {
    if (!role.is_default && role.position > rank) {
      rank = role.position;
    }
  }

  return rank;
}

export function canManageMemberRoles(mask: number, isOwner: boolean): boolean {
  return isOwner || hasPermission(mask, PERMISSIONS.ManageRoles);
}

export type RoleCaller = {
  rank: number;
  permissions: number;
  isOwner: boolean;
};

export type RoleToggleCheck = {
  allowed: boolean;
  reason: string | null;
};

// Mirrors AssignRoleHandler/UnassignRoleHandler: the default role is never
// togglable, hierarchy applies in both directions, and the "you must already
// hold every bit the role grants" check applies only when assigning.
export function canToggleRole(
  role: GuildRoleDto,
  isAssigned: boolean,
  caller: RoleCaller
): RoleToggleCheck {
  if (role.is_default) {
    return { allowed: false, reason: 'The default role cannot be assigned or removed.' };
  }

  if (caller.rank <= role.position) {
    return { allowed: false, reason: 'This role sits at or above your highest role.' };
  }

  if (!isAssigned && !hasPermission(caller.permissions, rolePermissionBits(role))) {
    return { allowed: false, reason: "This role grants permissions you don't have." };
  }

  return { allowed: true, reason: null };
}
