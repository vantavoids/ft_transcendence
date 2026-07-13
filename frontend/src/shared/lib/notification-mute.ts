// Pure mute helpers shared by the notification hook and the preferences store.
// Kept in their own module so those two can both import them without forming an
// import cycle.

import type {
  NotificationDto,
  NotificationPreferenceDto,
  NotificationScopeType
} from '../api/notification';

export type NotificationMuteScope = {
  scopeType: NotificationScopeType;
  scopeId: string;
};

export function isScopeMuted(
  preferences: NotificationPreferenceDto[],
  scopeType: NotificationScopeType,
  scopeId: string
): boolean {
  const now = Date.now();
  return preferences.some(
    (preference) =>
      preference.scope_type === scopeType &&
      preference.scope_id === scopeId &&
      preference.muted &&
      (preference.muted_until == null || Date.parse(preference.muted_until) > now)
  );
}

// the scopes a notification can be muted under; also drives the per-item mute buttons
export function getNotificationMuteScopes(notification: NotificationDto): NotificationMuteScope[] {
  switch (notification.type) {
    case 'mention':
      return [
        { scopeType: 'channel', scopeId: notification.payload.channel_id },
        { scopeType: 'guild', scopeId: notification.payload.guild_id }
      ];
    case 'guild_invite':
    case 'guild_welcome':
      // source_id carries the guild id for both types
      return notification.source_id
        ? [{ scopeType: 'guild', scopeId: notification.source_id }]
        : [];
    default:
      return [];
  }
}

export function isSuppressedByMute(
  notification: NotificationDto,
  preferences: NotificationPreferenceDto[]
): boolean {
  return getNotificationMuteScopes(notification).some(({ scopeType, scopeId }) =>
    isScopeMuted(preferences, scopeType, scopeId)
  );
}
