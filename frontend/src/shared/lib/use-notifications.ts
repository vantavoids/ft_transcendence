import { useCallback, useEffect, useRef, useState } from 'react';
import { dispatchFriendsChanged } from './friends-events';
import {
  buildNotificationEventsUrl,
  dismissNotification,
  getUnreadNotificationCount,
  listNotifications,
  markAllNotificationsRead,
  markNotificationRead,
  NOTIFICATION_RECEIVE_EVENT,
  type ListNotificationsQuery,
  type NotificationDto
} from '../api/notification';
import { getAccessToken } from './session';
import { useNotificationPrefs } from './notification-prefs-store';
import { isSuppressedByMute } from './notification-mute';

// re-exported so existing importers (notification-card) keep their import path
export {
  isScopeMuted,
  getNotificationMuteScopes,
  type NotificationMuteScope
} from './notification-mute';

const PAGE_SIZE = 50;
const RECONNECT_BASE_DELAY_MS = 2_000;
const RECONNECT_MAX_DELAY_MS = 30_000;

export type NotificationFilter = {
  unreadOnly: boolean;
  includeDismissed: boolean;
};

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
    unreadOnly: true,
    includeDismissed: false
  });
  // preferences (and their mutations) are owned by the shared store so the
  // notification panel, the sidebar dimming and the context menus stay in sync
  const { preferences, mute, unmute } = useNotificationPrefs();

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

      // transient "friends changed" signal (e.g. we were unfriended): not a
      // persisted notification, so re-fetch the friends list and stop before the
      // list/badge bookkeeping.
      if ((notification.type as string) === 'friends_changed') {
        dispatchFriendsChanged();
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
