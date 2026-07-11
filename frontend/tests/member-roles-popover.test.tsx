import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { createRef } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemberRoleChips, MemberRolesPopover } from '../src/components/guild/member-roles-popover';
import type { GuildRoleDto } from '../src/shared/api/guild';
import type { HydratedGuildMember } from '../src/shared/guilds/use-guild-members';
import { PERMISSIONS, type RoleCaller } from '../src/shared/guilds/role-permissions';

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

const everyone = makeRole({ id: 'r-everyone', name: '@everyone', position: 0, is_default: true });
const moderator = makeRole({ id: 'r-mod', name: 'Moderator', color: '#ff6188', position: 5 });
const helper = makeRole({ id: 'r-helper', name: 'Helper', color: '#a9dc76', position: 2 });
const overseer = makeRole({ id: 'r-overseer', name: 'Overseer', position: 9, permissions: '2048' });

function makeMember(roles: GuildRoleDto[]): HydratedGuildMember {
  return {
    userId: 'u-1',
    displayName: 'Newcomer',
    username: 'newcomer',
    avatarUrl: null,
    bannerUrl: null,
    bio: null,
    nickname: null,
    status: 'online',
    roles,
    joinedAt: '2026-07-11T00:00:00Z',
    isOwner: false,
    isDeleted: false
  };
}

const midCaller: RoleCaller = { rank: 6, permissions: PERMISSIONS.ManageRoles, isOwner: false };

function renderPopover(member: HydratedGuildMember, roles: GuildRoleDto[], caller: RoleCaller) {
  return renderToStaticMarkup(
    <MemberRolesPopover
      guildId="g-1"
      member={member}
      roles={roles}
      caller={caller}
      containerRef={createRef<HTMLDivElement>()}
      onChanged={() => undefined}
      onClose={() => undefined}
    />
  );
}

describe('MemberRoleChips', () => {
  it('renders one chip per role with its color dot', () => {
    const html = renderToStaticMarkup(<MemberRoleChips roles={[moderator, helper]} />);

    assert.ok(html.includes('Moderator'));
    assert.ok(html.includes('Helper'));
    assert.ok(html.includes('background-color:#ff6188'));
    assert.ok(html.includes('background-color:#a9dc76'));
  });

  it('renders nothing without roles', () => {
    assert.equal(renderToStaticMarkup(<MemberRoleChips roles={[]} />), '');
  });
});

describe('MemberRolesPopover', () => {
  it('lists non-default roles sorted by position descending', () => {
    const html = renderPopover(makeMember([]), [everyone, helper, moderator], midCaller);

    assert.ok(!html.includes('@everyone'));
    assert.ok(html.indexOf('Moderator') < html.indexOf('Helper'));
  });

  it('checks the roles the member holds', () => {
    const html = renderPopover(makeMember([helper]), [helper, moderator], midCaller);

    const checkboxes = html.match(/<input[^>]*type="checkbox"[^>]*>/g) ?? [];
    assert.equal(checkboxes.length, 2);
    assert.equal(checkboxes.filter((input) => input.includes('checked')).length, 1);
  });

  it('disables roles at or above the caller rank with a hierarchy hint', () => {
    const html = renderPopover(makeMember([]), [overseer], midCaller);

    assert.ok(html.includes('disabled'));
    assert.ok(html.includes('This role sits at or above your highest role.'));
  });

  it('disables assigning roles that grant bits the caller lacks', () => {
    const richRole = makeRole({ id: 'r-rich', name: 'Rich', position: 2, permissions: '2048' });
    const html = renderPopover(makeMember([]), [richRole], midCaller);

    assert.ok(html.includes('disabled'));
    assert.ok(html.includes('This role grants permissions you don&#x27;t have.'));
  });

  it('shows an empty state when only the default role exists', () => {
    const html = renderPopover(makeMember([]), [everyone], midCaller);

    assert.ok(html.includes('No roles yet. Create one in the Roles tab.'));
  });
});
