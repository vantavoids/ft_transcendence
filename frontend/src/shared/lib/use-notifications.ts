import { useCallback, useEffect, useRef, useState } from 'react';
import { ApiError } from '../api/client';
import { dispatchFriendsChanged } from './friends-events';
import {
  buildNotificationEventsUrl,
  deleteNotificationPreference,
  dismissNotification,
  getUnreadNotificationCount,
  listNotificationPreferences,
  listNotifications,
  markAllNotificationsRead,
  markNotificationRead,
  NOTIFICATION_RECEIVE_EVENT,
  setNotificationPreference,
  type ListNotificationsQuery,
  type NotificationDto,
  type NotificationPreferenceDto,
  type NotificationScopeType
} from '../api/notification';
import { getAccessToken } from './session';

const PAGE_SIZE = 50;
const RECONNECT_BASE_DELAY_MS = 2_000;
const RECONNECT_MAX_DELAY_MS = 30_000;

export type NotificationFilter = {
  unreadOnly: boolean;
  includeDismissed: boolean;
};

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

function isSuppressedByMute(
  notification: NotificationDto,
  preferences: NotificationPreferenceDto[]
): boolean {
  return getNotificationMuteScopes(notification).some(({ scopeType, scopeId }) =>
    isScopeMuted(preferences, scopeType, scopeId)
  );
}

function buildListQuery(filter: NotificationFilter): ListNotificationsQuery {
  return {
    // contract: read=false means unread only, omitted means all
    read: filter.unreadOnly ? false : undefined,
    include_dismissed: filter.includeDismissed ? true : undefined,
    limit: PAGE_SIZE
  };
}

export function useNotifications() {
  const [notifications, setNotifications] = useState<NotificationDto[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [hasMore, setHasMore] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingMore, setIsLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState<NotificationFilter>({
    unreadOnly: false,
    includeDismissed: false
  });
  const [preferences, setPreferences] = useState<NotificationPreferenceDto[]>([]);

  // bumped on every feed fetch; responses that come back after a newer fetch
  // started are discarded instead of clobbering the newer results
  const fetchSeqRef = useRef(0);

  const fetchFeed = useCallback(
    async (options?: { silent?: boolean }) => {
      const seq = ++fetchSeqRef.current;
      if (!options?.silent) {
        setIsLoading(true);
        setError(null);
      }
      try {
        const [items, unread] = await Promise.all([
          listNotifications(buildListQuery(filter)),
          getUnreadNotificationCount()
        ]);
        if (seq !== fetchSeqRef.current) {
          return;
        }
        setNotifications(items);
        setHasMore(items.length === PAGE_SIZE);
        setUnreadCount(unread.count);
      } catch {
        if (!options?.silent && seq === fetchSeqRef.current) {
          setError('Impossible de charger les notifications.');
        }
      } finally {
        if (!options?.silent && seq === fetchSeqRef.current) {
          setIsLoading(false);
        }
      }
    },
    [filter]
  );

  const refresh = useCallback(() => fetchFeed(), [fetchFeed]);

  useEffect(() => {
    void fetchFeed();
  }, [fetchFeed]);

  useEffect(() => {
    let active = true;
    listNotificationPreferences()
      .then((items) => {
        if (active) setPreferences(items);
      })
      .catch(() => {
        // best-effort: without preferences the server still filters muted inserts
      });
    return () => {
      active = false;
    };
  }, []);

  const loadMore = useCallback(async () => {
    const oldest = notifications[notifications.length - 1];
    if (!oldest || isLoadingMore) {
      return;
    }

    const seq = fetchSeqRef.current;
    setIsLoadingMore(true);
    try {
      const page = await listNotifications({ ...buildListQuery(filter), before: oldest.id });
      // a filter change refetched the feed while this page was in flight;
      // its items belong to the previous filter, drop them
      if (seq !== fetchSeqRef.current) {
        return;
      }
      setNotifications((current) => {
        const known = new Set(current.map((notification) => notification.id));
        return [...current, ...page.filter((notification) => !known.has(notification.id))];
      });
      setHasMore(page.length === PAGE_SIZE);
    } catch {
      if (seq === fetchSeqRef.current) {
        setError('Impossible de charger plus de notifications.');
      }
    } finally {
      setIsLoadingMore(false);
    }
  }, [notifications, isLoadingMore, filter]);

  // refs so the long-lived SSE handlers always see current state
  const fetchFeedRef = useRef(fetchFeed);
  useEffect(() => {
    fetchFeedRef.current = fetchFeed;
  }, [fetchFeed]);
  const preferencesRef = useRef(preferences);
  useEffect(() => {
    preferencesRef.current = preferences;
  }, [preferences]);
  // mirror of the current notification ids so the push handler can tell a
  // genuinely new notification from one the reconcile fetch already delivered
  const knownIdsRef = useRef<Set<string>>(new Set());
  useEffect(() => {
    knownIdsRef.current = new Set(notifications.map((notification) => notification.id));
  }, [notifications]);

  useEffect(() => {
    let disposed = false;
    let source: EventSource | null = null;
    let retryTimer: number | undefined;
    let attempt = 0;

    function handlePush(event: MessageEvent) {
      let notification: NotificationDto;
      try {
        notification = JSON.parse(String(event.data)) as NotificationDto;
      } catch {
        return;
      }

      // belt and braces: the server already skips muted inserts, but a mute
      // set in this tab may not have reached it before the push
      if (isSuppressedByMute(notification, preferencesRef.current)) {
        return;
      }

      // a push can race the on-open reconcile fetch (the row is persisted
      // before the push); if the fetch already delivered it, bumping the
      // badge again would overcount
      if (knownIdsRef.current.has(notification.id)) {
        return;
      }

      setUnreadCount((count) => count + 1);
      setNotifications((current) =>
        current.some((existing) => existing.id === notification.id)
          ? current
          : [notification, ...current]
      );

      // a friend request we sent was accepted: our friend graph changed, so
      // nudge the friends list to re-fetch.
      if (notification.type === 'friend_accept') {
        dispatchFriendsChanged();
      }
    }

    function scheduleReconnect() {
      attempt += 1;
      const delay = Math.min(RECONNECT_BASE_DELAY_MS * 2 ** (attempt - 1), RECONNECT_MAX_DELAY_MS);
      retryTimer = window.setTimeout(() => {
        // this request goes through apiFetch, which refreshes an expired
        // access token on 401 - so the stream reopens with valid credentials
        getUnreadNotificationCount()
          .then(({ count }) => setUnreadCount(count))
          .catch(() => {})
          .finally(() => connect());
      }, delay);
    }

    function connect() {
      if (disposed) {
        return;
      }
      const token = getAccessToken();
      if (!token) {
        return;
      }

      source = new EventSource(buildNotificationEventsUrl(token));
      source.addEventListener(NOTIFICATION_RECEIVE_EVENT, handlePush);
      source.onopen = () => {
        attempt = 0;
        // no Last-Event-ID replay: reconcile anything missed while the
        // stream was closed with a fresh fetch on every (re)open
        void fetchFeedRef.current({ silent: true });
      };
      source.onerror = () => {
        source?.close();
        source = null;
        if (!disposed) {
          scheduleReconnect();
        }
      };
    }

    connect();

    return () => {
      disposed = true;
      if (retryTimer !== undefined) {
        window.clearTimeout(retryTimer);
      }
      source?.close();
    };
  }, []);

  const markRead = useCallback(
    async (notificationId: string) => {
      const target = notifications.find((notification) => notification.id === notificationId);
      if (!target || target.read) {
        return;
      }

      setNotifications((current) =>
        current.map((notification) =>
          notification.id === notificationId ? { ...notification, read: true } : notification
        )
      );
      setUnreadCount((count) => Math.max(0, count - 1));

      try {
        await markNotificationRead(notificationId);
      } catch {
        void fetchFeed({ silent: true });
      }
    },
    [notifications, fetchFeed]
  );

  const markAllRead = useCallback(async () => {
    if (filter.unreadOnly) {
      // every row in this view just stopped matching the unread-only filter
      setNotifications([]);
      setHasMore(false);
    } else {
      setNotifications((current) =>
        current.map((notification) =>
          notification.read ? notification : { ...notification, read: true }
        )
      );
    }
    setUnreadCount(0);

    try {
      await markAllNotificationsRead();
    } catch {
      void fetchFeed({ silent: true });
    }
  }, [fetchFeed, filter.unreadOnly]);

  const dismiss = useCallback(
    async (notificationId: string) => {
      const target = notifications.find((notification) => notification.id === notificationId);
      if (!target) {
        return;
      }

      if (filter.includeDismissed) {
        // the server marks dismissed rows read in the same write
        setNotifications((current) =>
          current.map((notification) =>
            notification.id === notificationId
              ? { ...notification, read: true, dismissed_at: new Date().toISOString() }
              : notification
          )
        );
      } else {
        setNotifications((current) =>
          current.filter((notification) => notification.id !== notificationId)
        );
      }
      if (!target.read) {
        // the unread count excludes dismissed rows regardless of read state
        setUnreadCount((count) => Math.max(0, count - 1));
      }

      try {
        await dismissNotification(notificationId);
      } catch {
        void fetchFeed({ silent: true });
      }
    },
    [notifications, filter.includeDismissed, fetchFeed]
  );

  const mute = useCallback(
    async (scopeType: NotificationScopeType, scopeId: string, mutedUntil?: string | null) => {
      const updated = await setNotificationPreference(scopeType, scopeId, {
        muted: true,
        muted_until: mutedUntil ?? null
      });
      setPreferences((current) => [
        ...current.filter(
          (preference) => !(preference.scope_type === scopeType && preference.scope_id === scopeId)
        ),
        updated
      ]);
    },
    []
  );

  const unmute = useCallback(async (scopeType: NotificationScopeType, scopeId: string) => {
    try {
      await deleteNotificationPreference(scopeType, scopeId);
    } catch (err) {
      // 404 means no preference row, which is already the unmuted default
      if (!(err instanceof ApiError && err.status === 404)) {
        throw err;
      }
    }
    setPreferences((current) =>
      current.filter(
        (preference) => !(preference.scope_type === scopeType && preference.scope_id === scopeId)
      )
    );
  }, []);

  return {
    notifications,
    unreadCount,
    hasMore,
    isLoading,
    isLoadingMore,
    error,
    filter,
    setFilter,
    refresh,
    loadMore,
    markRead,
    markAllRead,
    dismiss,
    preferences,
    mute,
    unmute
  };
}

export type UseNotificationsResult = ReturnType<typeof useNotifications>;
