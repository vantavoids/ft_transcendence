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

  it('exposes every assigned role (minus @everyone) for the profile card', () => {
    const a = makeRole({ id: 'r-a', name: 'A', color: '#ff6188' });
    const b = makeRole({ id: 'r-b', name: 'B', color: '#a9dc76' });
    const everyone = makeRole({ id: 'r-everyone', name: 'everyone', is_default: true });
    const profile = toProfileMember(makeMember({ roles: [a, b, everyone] }));

    assert.deepEqual(
      profile.roles?.map((role) => role.name),
      ['A', 'B']
    );
    assert.deepEqual(
      profile.roles?.map((role) => role.color),
      ['#ff6188', '#a9dc76']
    );
  });
});

describe('buildMemberGroups', () => {
  const test = makeRole({ id: 'r-test', name: 'test', color: '#ff6188', is_hoisted: true, position: 5 });
  const plouf = makeRole({ id: 'r-plouf', name: 'plouf', color: '#a9dc76', is_hoisted: true, position: 2 });
  const tester = makeMember({ userId: 'u-tester', displayName: 'Tester', roles: [test] });
  const ploufer = makeMember({ userId: 'u-ploufer', displayName: 'Ploufer', roles: [plouf] });
  const owner = makeMember({ userId: 'u-owner', displayName: 'Owner', isOwner: true });
  const nobody = makeMember({ userId: 'u-nobody', displayName: 'Anna' });

  it('groups members under their highest hoisted role, ordered by position', () => {
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

  it('does not create a section for a non-hoisted role', () => {
    const casual = makeRole({ id: 'r-casual', name: 'casual', is_hoisted: false, position: 9 });
    const casualMember = makeMember({ userId: 'u-casual', displayName: 'Casual', roles: [casual] });

    const groups = buildMemberGroups([casualMember], true);

    assert.deepEqual(
      groups.map((group) => group.title),
      ['Members']
    );
    assert.deepEqual(
      groups[0].members.map((member) => member.displayName),
      ['Casual']
    );
  });

  it('groups under the highest hoisted role even when a non-hoisted role sits higher', () => {
    const bigNonHoist = makeRole({ id: 'r-big', name: 'Big', is_hoisted: false, position: 20 });
    const modHoist = makeRole({ id: 'r-mod', name: 'Mod', is_hoisted: true, position: 3 });
    const member = makeMember({ userId: 'u-mixed', displayName: 'Mixed', roles: [bigNonHoist, modHoist] });

    const groups = buildMemberGroups([member], true);

    assert.deepEqual(
      groups.map((group) => group.title),
      ['Mod']
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

  it('orders hoisted groups by position, highest first', () => {
    const zeta = makeRole({ id: 'r-z', name: 'zeta', is_hoisted: true, position: 1 });
    const alpha = makeRole({ id: 'r-a', name: 'alpha', is_hoisted: true, position: 2 });
    const groups = buildMemberGroups(
      [makeMember({ userId: 'u-1', roles: [zeta] }), makeMember({ userId: 'u-2', roles: [alpha] })],
      true
    );

    assert.deepEqual(
      groups.map((group) => group.title),
      ['alpha', 'zeta']
    );
  });

  it('breaks equal positions alphabetically', () => {
    const bb = makeRole({ id: 'r-bb', name: 'bb', is_hoisted: true, position: 5 });
    const aa = makeRole({ id: 'r-aa', name: 'aa', is_hoisted: true, position: 5 });
    const groups = buildMemberGroups(
      [makeMember({ userId: 'u-1', roles: [bb] }), makeMember({ userId: 'u-2', roles: [aa] })],
      true
    );

    assert.deepEqual(
      groups.map((group) => group.title),
      ['aa', 'bb']
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
