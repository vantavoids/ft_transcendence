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
import { getAccessToken } from './session';
import { isScopeMuted } from './use-notifications';
import { muteDurationToIso, type MuteDuration } from './mute-durations';

type NotificationPrefsValue = {
  /** True (and still in the future) if this scope is currently muted. */
  isMuted: (scopeType: NotificationScopeType, scopeId: string) => boolean;
  mute: (
    scopeType: NotificationScopeType,
    scopeId: string,
    duration: MuteDuration
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
    async (scopeType: NotificationScopeType, scopeId: string, duration: MuteDuration) => {
      const updated = await setNotificationPreference(scopeType, scopeId, {
        muted: true,
        muted_until: muteDurationToIso(duration)
      });
      setPreferences((current) => upsert(current, updated));
    },
    []
  );

  const unmute = useCallback(async (scopeType: NotificationScopeType, scopeId: string) => {
    await deleteNotificationPreference(scopeType, scopeId);
    setPreferences((current) => removeScope(current, scopeType, scopeId));
  }, []);

  const value = useMemo(() => ({ isMuted, mute, unmute }), [isMuted, mute, unmute]);

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
