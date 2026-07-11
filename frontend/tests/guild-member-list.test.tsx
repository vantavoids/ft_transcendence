import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { buildMemberGroups, toProfileMember } from '../src/components/guild-member-list';
import type { HydratedGuildMember } from '../src/shared/guilds/use-guild-members';
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

function makeMember(overrides: Partial<HydratedGuildMember> = {}): HydratedGuildMember {
  return {
    userId: 'u-1',
    displayName: 'Newcomer',
    username: 'newcomer',
    avatarUrl: null,
    bannerUrl: null,
    bio: null,
    nickname: null,
    status: 'online',
    roles: [],
    joinedAt: '2026-07-11T00:00:00Z',
    isOwner: false,
    isDeleted: false,
    ...overrides
  };
}

describe('toProfileMember', () => {
  it('uses the highest role name and color instead of a generic "Admin"', () => {
    const test = makeRole({ id: 'r-test', name: 'test', color: '#ff6188', position: 7 });
    const plouf = makeRole({ id: 'r-plouf', name: 'plouf', color: '#a9dc76', position: 2 });
    const profile = toProfileMember(makeMember({ roles: [test, plouf] }));

    assert.equal(profile.role, 'test');
    assert.equal(profile.roleColor, '#ff6188');
  });

  it('labels the owner as Owner without a role color', () => {
    const profile = toProfileMember(
      makeMember({ isOwner: true, roles: [makeRole({ name: 'test', color: '#ff6188' })] })
    );

    assert.equal(profile.role, 'Owner');
    assert.equal(profile.roleColor, null);
  });

  it('falls back to Member when no roles are assigned', () => {
    const profile = toProfileMember(makeMember());

    assert.equal(profile.role, 'Member');
    assert.equal(profile.roleColor, null);
  });
});

describe('buildMemberGroups', () => {
  const test = makeRole({ id: 'r-test', name: 'test', color: '#ff6188', permissions: '4095' });
  const plouf = makeRole({ id: 'r-plouf', name: 'plouf', color: '#a9dc76', permissions: '0' });
  const tester = makeMember({ userId: 'u-tester', displayName: 'Tester', roles: [test] });
  const ploufer = makeMember({ userId: 'u-ploufer', displayName: 'Ploufer', roles: [plouf] });
  const owner = makeMember({ userId: 'u-owner', displayName: 'Owner', isOwner: true });
  const nobody = makeMember({ userId: 'u-nobody', displayName: 'Anna' });

  it('groups each member under their top role, ordered by permission count', () => {
    const groups = buildMemberGroups([nobody, ploufer, tester], true);

    assert.deepEqual(
      groups.map((group) => group.title),
      ['test', 'plouf', 'Members']
    );
    assert.equal(groups[0].roleColor, '#ff6188');
    assert.deepEqual(
      groups[0].members.map((member) => member.displayName),
      ['Tester']
    );
    assert.deepEqual(
      groups[2].members.map((member) => member.displayName),
      ['Anna']
    );
  });

  it('pins the owner in an Owner group at the top', () => {
    const groups = buildMemberGroups([tester, owner], true);

    assert.deepEqual(
      groups.map((group) => group.title),
      ['Owner', 'test']
    );
    assert.deepEqual(
      groups[0].members.map((member) => member.displayName),
      ['Owner']
    );
  });

  it('keeps the owner on top even when another role grants more permissions', () => {
    const adminRole = makeRole({ id: 'r-admin', name: 'Administrator', permissions: '256' });
    const richOwner = makeMember({
      userId: 'u-owner',
      displayName: 'Owner',
      isOwner: true,
      roles: [adminRole]
    });

    // "test" has all 12 permission flags; the owner's Administrator role has 1
    const groups = buildMemberGroups([tester, richOwner], true);

    assert.deepEqual(
      groups.map((group) => group.title),
      ['Owner', 'test']
    );
  });

  it('breaks equal permission counts alphabetically', () => {
    const zeta = makeRole({ id: 'r-z', name: 'zeta', permissions: '1' });
    const alpha = makeRole({ id: 'r-a', name: 'alpha', permissions: '2' });
    const groups = buildMemberGroups(
      [makeMember({ userId: 'u-1', roles: [zeta] }), makeMember({ userId: 'u-2', roles: [alpha] })],
      true
    );

    assert.deepEqual(
      groups.map((group) => group.title),
      ['alpha', 'zeta']
    );
  });

  it('merges everyone into one alphabetical list when grouping is off', () => {
    const groups = buildMemberGroups([tester, nobody, ploufer], false);

    assert.equal(groups.length, 1);
    assert.deepEqual(
      groups[0].members.map((member) => member.displayName),
      ['Anna', 'Ploufer', 'Tester']
    );
  });

  it('returns no groups for an empty member list', () => {
    assert.deepEqual(buildMemberGroups([], false), []);
    assert.deepEqual(buildMemberGroups([], true), []);
  });
});
