import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { toProfileMember } from '../src/components/guild-member-list';
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
