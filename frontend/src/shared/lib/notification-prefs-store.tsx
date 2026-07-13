'use client';

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import {
  deleteNotificationPreference,
  listNotificationPreferences,
  setNotificationPreference,
  type NotificationPreferenceDto,
  type NotificationScopeType
} from '../api/notification';
import { ApiError } from '../api/client';
import { getAccessToken } from './session';
import { isScopeMuted } from './notification-mute';

type NotificationPrefsValue = {
  /** The raw preference rows, so consumers can filter live SSE inserts. */
  preferences: NotificationPreferenceDto[];
  /** True (and still in the future) if this scope is currently muted. */
  isMuted: (scopeType: NotificationScopeType, scopeId: string) => boolean;
  mute: (
    scopeType: NotificationScopeType,
    scopeId: string,
    mutedUntil: string | null
  ) => Promise<void>;
  unmute: (scopeType: NotificationScopeType, scopeId: string) => Promise<void>;
};

const NotificationPrefsContext = createContext<NotificationPrefsValue | null>(null);

function hasSession() {
  if (typeof window === 'undefined') {
    return false;
  }
  return Boolean(getAccessToken());
}

// replace the entry for a scope (drop any existing one first) so optimistic
// updates never leave a stale duplicate behind.
function upsert(
  preferences: NotificationPreferenceDto[],
  next: NotificationPreferenceDto
): NotificationPreferenceDto[] {
  const without = preferences.filter(
    (pref) => !(pref.scope_type === next.scope_type && pref.scope_id === next.scope_id)
  );
  return [...without, next];
}

function removeScope(
  preferences: NotificationPreferenceDto[],
  scopeType: NotificationScopeType,
  scopeId: string
): NotificationPreferenceDto[] {
  return preferences.filter(
    (pref) => !(pref.scope_type === scopeType && pref.scope_id === scopeId)
  );
}

export function NotificationPrefsProvider({ children }: { children: ReactNode }) {
  const [preferences, setPreferences] = useState<NotificationPreferenceDto[]>([]);

  useEffect(() => {
    if (!hasSession()) {
      return;
    }

    let cancelled = false;
    listNotificationPreferences()
      .then((prefs) => {
        if (!cancelled) {
          setPreferences(prefs);
        }
      })
      .catch(() => {
        // best effort: an empty list just means nothing renders as muted
      });

    return () => {
      cancelled = true;
    };
  }, []);

  const isMuted = useCallback(
    (scopeType: NotificationScopeType, scopeId: string) =>
      isScopeMuted(preferences, scopeType, scopeId),
    [preferences]
  );

  const mute = useCallback(
    async (scopeType: NotificationScopeType, scopeId: string, mutedUntil: string | null) => {
      const updated = await setNotificationPreference(scopeType, scopeId, {
        muted: true,
        muted_until: mutedUntil
      });
      setPreferences((current) => upsert(current, updated));
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
    setPreferences((current) => removeScope(current, scopeType, scopeId));
  }, []);

  const value = useMemo(
    () => ({ preferences, isMuted, mute, unmute }),
    [preferences, isMuted, mute, unmute]
  );

  return (
    <NotificationPrefsContext.Provider value={value}>{children}</NotificationPrefsContext.Provider>
  );
}

export function useNotificationPrefs() {
  const context = useContext(NotificationPrefsContext);

  if (!context) {
    throw new Error('useNotificationPrefs must be used within a NotificationPrefsProvider.');
  }

  return context;
}
