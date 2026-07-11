import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { renderToStaticMarkup } from 'react-dom/server';
import { NotificationCard } from '../src/components/notification-card';
import type { NotificationDto } from '../src/shared/api/notification';
import type { UseNotificationsResult } from '../src/shared/lib/use-notifications';

const dmNotification: NotificationDto = {
  id: '900',
  type: 'dm',
  actor_id: '456',
  source_id: '789',
  payload: { conversation_id: '111', preview: 'Salut !' },
  read: false,
  created_at: '2026-07-11T00:00:00Z'
};

const friendRequestNotification: NotificationDto = {
  id: '901',
  type: 'friend_request',
  actor_id: '456',
  source_id: '222',
  payload: {},
  read: false,
  created_at: '2026-07-11T00:00:00Z'
};

function buildFeed(notifications: NotificationDto[]): UseNotificationsResult {
  return {
    notifications,
    unreadCount: notifications.filter((notification) => !notification.read).length,
    hasMore: false,
    isLoading: false,
    isLoadingMore: false,
    error: null,
    filter: { unreadOnly: false, includeDismissed: false },
    setFilter: () => undefined,
    refresh: () => Promise.resolve(),
    loadMore: () => Promise.resolve(),
    markRead: () => Promise.resolve(),
    markAllRead: () => Promise.resolve(),
    dismiss: () => Promise.resolve(),
    preferences: [],
    mute: () => Promise.resolve(),
    unmute: () => Promise.resolve()
  };
}

describe('NotificationCard', () => {
  it('renders an open-conversation link only for message-backed notifications', () => {
    const html = renderToStaticMarkup(
      <NotificationCard
        feed={buildFeed([dmNotification, friendRequestNotification])}
        onClose={() => undefined}
        onOpenNotification={() => undefined}
      />
    );

    const openLinkCount = html.split('aria-label="Ouvrir la conversation"').length - 1;
    assert.equal(openLinkCount, 1);
    assert.ok(html.includes('Message prive'));
    assert.ok(html.includes('Demande d ami'));
  });
});
