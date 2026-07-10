import { apiFetch } from './client';
import { API_BASE_URL } from '../config/env';

export type NotificationType =
  | 'mention'
  | 'dm'
  | 'friend_request'
  | 'guild_invite'
  | 'guild_welcome'
  | 'incoming_call';

export type NotificationPayloadMap = {
  mention: { channel_id: string; guild_id: string; preview: string };
  dm: { conversation_id: string; preview: string };
  friend_request: Record<string, never>;
  guild_invite: { guild_name: string };
  guild_welcome: { guild_name: string };
  incoming_call: { call_type: 'audio' | 'video' };
};

// `user_id` is absent on SSE pushes (implicit from the connection) and
// `dismissed_at` only appears on the REST list, so both stay optional.
export type NotificationDto = {
  [K in NotificationType]: {
    id: string;
    user_id?: string;
    type: K;
    actor_id: string | null;
    source_id: string | null;
    payload: NotificationPayloadMap[K];
    read: boolean;
    dismissed_at?: string | null;
    created_at: string;
  };
}[NotificationType];

export type ListNotificationsQuery = {
  read?: boolean;
  include_dismissed?: boolean;
  limit?: number;
  before?: string;
};

export type MarkNotificationReadResponse = {
  id: string;
  read: true;
};

export type MarkAllNotificationsReadResponse = {
  updated: number;
};

export type UnreadCountResponse = {
  count: number;
};

export type NotificationScopeType = 'guild' | 'channel';

export type NotificationPreferenceDto = {
  scope_type: NotificationScopeType;
  scope_id: string;
  muted: boolean;
  // null for indefinite mutes; kept optional so an omitted field (older
  // service builds serialized with omitempty) reads the same as null
  muted_until?: string | null;
};

export type UpdateNotificationPreferencePayload = {
  muted: boolean;
  muted_until?: string | null;
};

export function listNotifications(query?: ListNotificationsQuery) {
  return apiFetch<NotificationDto[]>('notification', '/notifications', { query });
}

export function getUnreadNotificationCount() {
  return apiFetch<UnreadCountResponse>('notification', '/notifications/unread-count');
}

export function markNotificationRead(notificationId: string) {
  return apiFetch<MarkNotificationReadResponse>(
    'notification',
    `/notifications/${notificationId}/read`,
    {
      method: 'PATCH'
    }
  );
}

export function markAllNotificationsRead() {
  return apiFetch<MarkAllNotificationsReadResponse>('notification', '/notifications/read-all', {
    method: 'PATCH'
  });
}

export function dismissNotification(notificationId: string) {
  return apiFetch<void>('notification', `/notifications/${notificationId}`, {
    method: 'DELETE'
  });
}

export function listNotificationPreferences() {
  return apiFetch<NotificationPreferenceDto[]>('notification', '/notifications/preferences');
}

export function setNotificationPreference(
  scopeType: NotificationScopeType,
  scopeId: string,
  payload: UpdateNotificationPreferencePayload
) {
  return apiFetch<NotificationPreferenceDto>(
    'notification',
    `/notifications/preferences/${scopeType}/${scopeId}`,
    {
      method: 'PUT',
      body: payload
    }
  );
}

export function deleteNotificationPreference(scopeType: NotificationScopeType, scopeId: string) {
  return apiFetch<void>('notification', `/notifications/preferences/${scopeType}/${scopeId}`, {
    method: 'DELETE'
  });
}

// live delivery is a named SSE event, not the default `message` handler:
// EventSource.addEventListener(NOTIFICATION_RECEIVE_EVENT, ...)
export const NOTIFICATION_RECEIVE_EVENT = 'ReceiveNotification';

// EventSource cannot carry an Authorization header, so the contract passes
// the access token as a query parameter.
export function buildNotificationEventsUrl(accessToken: string) {
  return `${API_BASE_URL}/notification/v1/notifications/events?access_token=${encodeURIComponent(
    accessToken
  )}`;
}
