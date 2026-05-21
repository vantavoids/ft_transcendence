import { apiFetch } from './client';

export type NotificationType =
  | 'mention'
  | 'dm'
  | 'friend_request'
  | 'guild_invite'
  | 'guild_welcome'
  | 'incoming_call';

export type NotificationDto = {
  id: string;
  user_id: string;
  type: NotificationType;
  payload: Record<string, unknown>;
  read: boolean;
  created_at: string;
};

export type MarkNotificationReadResponse = {
  id: string;
  read: true;
};

export type MarkAllNotificationsReadResponse = {
  updated: number;
};

export function listNotifications(query?: { read?: boolean; limit?: number; before?: string }) {
  return apiFetch<NotificationDto[]>('notification', '/notifications', { query });
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
