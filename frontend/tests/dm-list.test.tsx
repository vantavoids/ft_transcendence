import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { renderToStaticMarkup } from 'react-dom/server';
import { DmList, type DirectMessage } from '../src/components/dm-list';
import type { CurrentUserProfile } from '../src/shared/mappers/user';

const currentUser: CurrentUserProfile = {
  id: '123',
  username: 'skydogzz',
  displayName: 'SkyDogzz',
  avatarUrl: 'https://cdn.example/current-avatar.png',
  bannerUrl: null,
  status: 'online',
  bio: null,
  lastSeenAt: '2026-07-11T00:00:00Z',
  lastSeenLabel: '11 Jul 00:00'
};

const directMessages: DirectMessage[] = [
  {
    id: '456',
    name: 'Octavia',
    status: 'online',
    accent: 'aqua',
    lastMessage: 'See you there',
    lastMessageAt: '12:45',
    lastActivityAt: Date.now(),
    unreadCount: 0,
    isArchived: false,
    avatarUrl: 'https://cdn.example/dm-avatar.png',
    bannerUrl: null,
    bio: 'DM profile'
  }
];

describe('DmList', () => {
  it('renders avatars for direct message rows and the current user footer', () => {
    const html = renderToStaticMarkup(
      <DmList
        activeDm="456"
        directMessages={directMessages}
        showArchived={false}
        friends={[]}
        mobilePane="messages"
        currentUser={currentUser}
        isMicMuted={false}
        isDeafened={false}
        unreadNotifications={0}
        onToggleDeafen={() => undefined}
        onToggleMicMute={() => undefined}
        onOpenNotifications={() => undefined}
        onOpenSettings={() => undefined}
        onOpenFriendProfile={() => undefined}
        onSelectDm={() => undefined}
        onToggleShowArchived={() => undefined}
        onArchiveDm={() => undefined}
      />
    );

    assert.ok(html.includes('https://cdn.example/dm-avatar.png'));
    assert.ok(html.includes('https://cdn.example/current-avatar.png'));
  });
});
