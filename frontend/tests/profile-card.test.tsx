import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { renderToStaticMarkup } from 'react-dom/server';
import { ProfileCard, type ProfileRoleManagement } from '../src/components/profile-card';
import type { GuildMember } from '../src/components/guild-member-list';
import type { HydratedGuildMember } from '../src/shared/guilds/use-guild-members';
import { PERMISSIONS } from '../src/shared/guilds/role-permissions';

const member: GuildMember = {
  id: '123',
  name: 'SkyDogzz',
  role: 'Owner',
  status: 'online',
  accent: 'aqua',
  activity: 'Joined 11 Jul 2026',
  bio: 'Shipping profile data.',
  avatarUrl: 'https://cdn.example/avatar.png',
  bannerUrl: 'https://cdn.example/banner.png'
};

describe('ProfileCard', () => {
  it('renders avatar, banner, and bio data', () => {
    const html = renderToStaticMarkup(<ProfileCard member={member} onClose={() => undefined} />);

    assert.ok(html.includes('https://cdn.example/avatar.png'));
    assert.ok(html.includes('https://cdn.example/banner.png'));
    assert.ok(html.includes('Shipping profile data.'));
  });

  it('renders a fallback when no bio is set', () => {
    const fallbackHtml = renderToStaticMarkup(
      <ProfileCard member={{ ...member, bio: null }} onClose={() => undefined} />
    );

    assert.ok(fallbackHtml.includes('No bio set.'));
  });

  it('renders the actual role name tinted with the role color', () => {
    const html = renderToStaticMarkup(
      <ProfileCard
        member={{ ...member, role: 'test', roleColor: '#ff6188' }}
        onClose={() => undefined}
      />
    );

    assert.ok(html.includes('>test</span>'));
    assert.ok(html.includes('color:#ff6188'));
    assert.ok(html.includes('border-color:#ff618859'));
    assert.ok(html.includes('background-color:#ff61881a'));
  });

  it('keeps the neutral token when no role color is set', () => {
    const html = renderToStaticMarkup(<ProfileCard member={member} onClose={() => undefined} />);

    assert.ok(html.includes('>Owner</span>'));
    assert.ok(!html.includes('border-color'));
  });

  it('renders a profile actions menu for other users', () => {
    const html = renderToStaticMarkup(
      <ProfileCard
        member={member}
        currentUserId="456"
        onAddFriend={() => undefined}
        onBlock={() => undefined}
        onClose={() => undefined}
      />
    );

    assert.ok(html.includes('Profile actions'));
    assert.ok(!html.includes('Add friend'));
    assert.ok(!html.includes('Block'));
  });

  it('hides relationship actions on the current user profile', () => {
    const html = renderToStaticMarkup(
      <ProfileCard
        member={member}
        currentUserId={member.id}
        onAddFriend={() => undefined}
        onBlock={() => undefined}
        onClose={() => undefined}
      />
    );

    assert.ok(!html.includes('Profile actions'));
    assert.ok(!html.includes('Add friend'));
    assert.ok(!html.includes('Block'));
  });

  it('lists every assigned role in a Roles section', () => {
    const html = renderToStaticMarkup(
      <ProfileCard
        member={{
          ...member,
          roles: [
            { id: 'r-a', name: 'Maintainer', color: '#ff6188' },
            { id: 'r-b', name: 'Reviewer', color: null }
          ]
        }}
        onClose={() => undefined}
      />
    );

    assert.ok(html.includes('Roles'));
    assert.ok(html.includes('>Maintainer</span>'));
    assert.ok(html.includes('>Reviewer</span>'));
    assert.ok(html.includes('background-color:#ff6188'));
  });

  it('omits the Roles section when the member has no roles', () => {
    const html = renderToStaticMarkup(<ProfileCard member={member} onClose={() => undefined} />);

    assert.ok(!html.includes('Roles'));
  });

  it('offers a Manage control and live roles when the viewer can manage roles', () => {
    const liveMember: HydratedGuildMember = {
      userId: '123',
      displayName: 'SkyDogzz',
      username: 'skydogzz',
      avatarUrl: null,
      bannerUrl: null,
      bio: null,
      nickname: null,
      status: 'online',
      roles: [
        {
          id: 'r-live',
          guild_id: 'g-1',
          name: 'LiveRole',
          color: '#78dce8',
          permissions: '0',
          position: 3,
          is_hoisted: false,
          is_mentionable: false,
          is_default: false
        }
      ],
      joinedAt: '2026-07-11T00:00:00Z',
      isOwner: false,
      isDeleted: false
    };

    const roleManagement: ProfileRoleManagement = {
      guildId: 'g-1',
      member: liveMember,
      roles: liveMember.roles,
      caller: { rank: 10, permissions: PERMISSIONS.ManageRoles, isOwner: false },
      onChanged: () => undefined
    };

    const html = renderToStaticMarkup(
      <ProfileCard member={member} roleManagement={roleManagement} onClose={() => undefined} />
    );

    // read-only chip reflects the live guild-member roles, and a Manage toggle appears
    assert.ok(html.includes('>LiveRole</span>'));
    assert.ok(html.includes('Manage'));
  });
});
