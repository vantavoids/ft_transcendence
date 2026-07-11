import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { hydrateGuildMembers } from '../src/shared/guilds/use-guild-members';
import type { GuildMemberDto, GuildRoleDto } from '../src/shared/api/guild';
import type { UserSummaryDto } from '../src/shared/api/user';

const memberRows: GuildMemberDto[] = [
  {
    user_id: 'u-1',
    nickname: null,
    roles: ['r-1'],
    joined_at: '2026-07-11T00:00:00Z'
  }
];

const guildRoles: GuildRoleDto[] = [
  {
    id: 'r-1',
    guild_id: 'g-1',
    name: 'Moderator',
    color: '#ffffff',
    permissions: '0',
    position: 10,
    is_hoisted: false,
    is_mentionable: false,
    is_default: false
  }
];

const user: UserSummaryDto = {
  id: 'u-1',
  username: 'newcomer',
  display_name: 'Newcomer',
  avatar_url: null,
  banner_url: null,
  status: 'online',
  bio: null
};

describe('hydrateGuildMembers', () => {
  it('keeps a member visible when their profile was recovered', () => {
    const hydrated = hydrateGuildMembers({
      memberRows,
      guildRoles,
      users: [user]
    });

    assert.equal(hydrated[0].displayName, 'Newcomer');
    assert.equal(hydrated[0].username, 'newcomer');
    assert.equal(hydrated[0].isDeleted, false);
  });

  it('only marks a member as deleted when deletion is confirmed', () => {
    const hydrated = hydrateGuildMembers({
      memberRows,
      guildRoles,
      users: [],
      deletedUserIds: new Set(['u-1'])
    });

    assert.equal(hydrated[0].displayName, 'Deleted User');
    assert.equal(hydrated[0].isDeleted, true);
  });

  it('does not default to Deleted User for an unresolved profile', () => {
    const hydrated = hydrateGuildMembers({
      memberRows,
      guildRoles,
      users: []
    });

    assert.equal(hydrated[0].displayName, 'Member');
    assert.equal(hydrated[0].isDeleted, false);
  });

  it('orders member roles by permission count, not position', () => {
    const roles: GuildRoleDto[] = [
      { ...guildRoles[0], id: 'r-high-pos', name: 'plouf', permissions: '0', position: 10 },
      { ...guildRoles[0], id: 'r-full', name: 'test', permissions: '4095', position: 2 }
    ];
    const hydrated = hydrateGuildMembers({
      memberRows: [{ ...memberRows[0], roles: ['r-high-pos', 'r-full'] }],
      guildRoles: roles,
      users: [user]
    });

    assert.deepEqual(
      hydrated[0].roles.map((role) => role.name),
      ['test', 'plouf']
    );
  });
});
