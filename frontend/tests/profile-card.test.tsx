import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { renderToStaticMarkup } from 'react-dom/server';
import { ProfileCard } from '../src/components/profile-card';
import type { GuildMember } from '../src/components/guild-member-list';

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
});
