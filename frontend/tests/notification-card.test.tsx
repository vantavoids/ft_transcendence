import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { createRef } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { NotificationCard, describeNotification } from '../src/components/notification-card';
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

const incomingCallNotification: NotificationDto = {
  id: '902',
  type: 'incoming_call',
  actor_id: '456',
  source_id: null,
  payload: { call_type: 'audio' },
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
  it('renders a typed open link for targetable notifications only', () => {
    const html = renderToStaticMarkup(
      <NotificationCard
        feed={buildFeed([dmNotification, friendRequestNotification, incomingCallNotification])}
        anchorRef={createRef<HTMLButtonElement>()}
        onClose={() => undefined}
        onOpenNotification={() => undefined}
      />
    );

    assert.equal(html.split('aria-label="Ouvrir la conversation"').length - 1, 1);
    assert.equal(html.split('aria-label="Voir la demande d ami"').length - 1, 1);
    // an incoming call has nowhere to deep-link to
    assert.equal(html.split('aria-label="Ouvrir').length - 1, 1);
    assert.ok(html.includes('Private message'));
    assert.ok(html.includes('Friend request'));
    assert.ok(html.includes('Incoming call'));
  });
});

describe('describeNotification', () => {
  it('names the author when the actor is resolved', () => {
    assert.equal(describeNotification(dmNotification, 'Testa').title, 'Private message from Testa');
    assert.equal(
      describeNotification(friendRequestNotification, 'Testa').detail,
      'Testa wants to add you as a friend.'
    );
    assert.equal(
      describeNotification(incomingCallNotification, 'Testa').detail,
      'Incoming audio call from Testa.'
    );
    assert.equal(
      describeNotification(
        {
          ...dmNotification,
          id: '903',
          type: 'mention',
          payload: { channel_id: '1', guild_id: '2', preview: 'hey' }
        },
        'Testa'
      ).title,
      'Mention from Testa'
    );
  });

  it('keeps actor-less wording while the author is unresolved', () => {
    assert.equal(describeNotification(dmNotification, null).title, 'Private message');
    assert.equal(
      describeNotification(friendRequestNotification, null).detail,
      'Someone wants to add you as a friend.'
    );
    assert.equal(
      describeNotification(incomingCallNotification, null).detail,
      'Incoming audio call.'
    );
  });
});
