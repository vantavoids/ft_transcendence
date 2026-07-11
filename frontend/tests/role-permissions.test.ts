import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  PERMISSIONS,
  canManageMemberRoles,
  canToggleRole,
  countPermissionBits,
  effectivePermissions,
  hasPermission,
  memberRank,
  sortRolesByDisplayPriority,
  type RoleCaller
} from '../src/shared/guilds/role-permissions';
import type { GuildRoleDto } from '../src/shared/api/guild';

function makeRole(overrides: Partial<GuildRoleDto> = {}): GuildRoleDto {
  return {
    id: 'r-1',
    guild_id: 'g-1',
    name: 'Role',
    color: '#78dce8',
    permissions: '0',
    position: 1,
    is_hoisted: false,
    is_mentionable: false,
    is_default: false,
    ...overrides
  };
}

const everyone = makeRole({
  id: 'r-everyone',
  name: '@everyone',
  permissions: '515',
  position: 0,
  is_default: true
});
const moderator = makeRole({ id: 'r-mod', name: 'Moderator', permissions: '112', position: 5 });
const admin = makeRole({ id: 'r-admin', name: 'Administrator', permissions: '256', position: 10 });

describe('memberRank', () => {
  it('gives the owner the highest possible rank', () => {
    assert.equal(memberRank([admin], true), Infinity);
    assert.equal(memberRank([], true), Infinity);
  });

  it('gives a member with no roles the lowest possible rank', () => {
    assert.equal(memberRank([], false), -Infinity);
  });

  it('ignores the default role and picks the highest position', () => {
    assert.equal(memberRank([everyone, moderator], false), 5);
    assert.equal(memberRank([everyone], false), -Infinity);
    assert.equal(memberRank([moderator, admin], false), 10);
  });
});

describe('effectivePermissions', () => {
  it('unions the default role with assigned roles', () => {
    assert.equal(effectivePermissions([moderator], [everyone, moderator, admin], false), 515 | 112);
  });

  it('includes only the default role when nothing is assigned', () => {
    assert.equal(effectivePermissions([], [everyone, moderator, admin], false), 515);
  });

  it('resolves the owner to Administrator', () => {
    assert.equal(effectivePermissions([], [everyone], true), PERMISSIONS.Administrator);
  });
});

describe('hasPermission', () => {
  it('lets the Administrator bit grant everything', () => {
    assert.equal(hasPermission(PERMISSIONS.Administrator, 2048), true);
  });

  it('requires every requested bit otherwise', () => {
    assert.equal(hasPermission(64 | 16, 64), true);
    assert.equal(hasPermission(64 | 16, 64 | 32), false);
  });
});

describe('canManageMemberRoles', () => {
  it('allows the owner, Administrator and ManageRoles holders', () => {
    assert.equal(canManageMemberRoles(0, true), true);
    assert.equal(canManageMemberRoles(PERMISSIONS.Administrator, false), true);
    assert.equal(canManageMemberRoles(PERMISSIONS.ManageRoles, false), true);
  });

  it('rejects everyone else', () => {
    assert.equal(canManageMemberRoles(515, false), false);
  });
});

describe('countPermissionBits', () => {
  it('counts the permission flags set on a mask', () => {
    assert.equal(countPermissionBits(0), 0);
    assert.equal(countPermissionBits(256), 1);
    assert.equal(countPermissionBits(64 | 16 | 32), 3);
    assert.equal(countPermissionBits(4095), 12);
  });
});

describe('sortRolesByDisplayPriority', () => {
  it('puts the role with the most permissions first, regardless of position', () => {
    const full = makeRole({ id: 'r-full', name: 'test', permissions: '4095', position: 1 });
    const none = makeRole({ id: 'r-none', name: 'plouf', permissions: '0', position: 9 });
    const some = makeRole({ id: 'r-some', name: 'mid', permissions: '112', position: 5 });

    const sorted = sortRolesByDisplayPriority([none, some, full], []);

    assert.deepEqual(
      sorted.map((role) => role.name),
      ['test', 'mid', 'plouf']
    );
  });

  it('breaks permission-count ties by assignment recency (API order)', () => {
    const older = makeRole({ id: 'r-older', name: 'older', permissions: '3' });
    const newer = makeRole({ id: 'r-newer', name: 'newer', permissions: '5' });

    // the API lists role ids most-recently-assigned first
    const sorted = sortRolesByDisplayPriority([older, newer], ['r-newer', 'r-older']);

    assert.deepEqual(
      sorted.map((role) => role.name),
      ['newer', 'older']
    );
  });

  it('falls back to alphabetical order when count and recency do not separate', () => {
    const zeta = makeRole({ id: 'r-z', name: 'zeta', permissions: '1' });
    const alpha = makeRole({ id: 'r-a', name: 'alpha', permissions: '2' });

    const sorted = sortRolesByDisplayPriority([zeta, alpha], []);

    assert.deepEqual(
      sorted.map((role) => role.name),
      ['alpha', 'zeta']
    );
  });
});

describe('canToggleRole', () => {
  const midCaller: RoleCaller = {
    rank: 5,
    permissions: 515 | 112 | PERMISSIONS.ManageRoles,
    isOwner: false
  };
  const owner: RoleCaller = {
    rank: Infinity,
    permissions: PERMISSIONS.Administrator,
    isOwner: true
  };

  it('never allows toggling the default role', () => {
    assert.equal(canToggleRole(everyone, false, owner).allowed, false);
    assert.equal(canToggleRole(everyone, true, owner).allowed, false);
  });

  it('blocks roles at or above the caller rank in both directions', () => {
    const sameRank = makeRole({ position: 5 });
    assert.equal(canToggleRole(sameRank, false, midCaller).allowed, false);
    assert.equal(canToggleRole(sameRank, true, midCaller).allowed, false);
    assert.match(canToggleRole(sameRank, false, midCaller).reason ?? '', /at or above/);
  });

  it('allows a lower role the caller can fully grant', () => {
    const lower = makeRole({ position: 2, permissions: '112' });
    assert.deepEqual(canToggleRole(lower, false, midCaller), { allowed: true, reason: null });
  });

  it('blocks assigning a role granting bits the caller lacks but allows unassigning it', () => {
    const banRole = makeRole({ position: 2, permissions: '2048' });
    assert.equal(canToggleRole(banRole, false, midCaller).allowed, false);
    assert.match(canToggleRole(banRole, false, midCaller).reason ?? '', /grants permissions/);
    assert.equal(canToggleRole(banRole, true, midCaller).allowed, true);
  });

  it('lets Administrator bypass the grant check but not the hierarchy', () => {
    const adminCaller: RoleCaller = {
      rank: 5,
      permissions: PERMISSIONS.Administrator,
      isOwner: false
    };
    const lowRich = makeRole({ position: 2, permissions: '4095' });
    const highRole = makeRole({ position: 10, permissions: '0' });
    assert.equal(canToggleRole(lowRich, false, adminCaller).allowed, true);
    assert.equal(canToggleRole(highRole, false, adminCaller).allowed, false);
  });

  it('allows the owner to toggle any non-default role', () => {
    assert.equal(canToggleRole(admin, false, owner).allowed, true);
    assert.equal(canToggleRole(moderator, true, owner).allowed, true);
  });
});
