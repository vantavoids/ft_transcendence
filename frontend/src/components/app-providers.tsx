'use client';

import type { ReactNode } from 'react';
import { GuildProvider } from '../shared/guilds/guild-store';
import { CurrentUserProvider } from '../shared/user/user-store';
import { NotificationPrefsProvider } from '../shared/lib/notification-prefs-store';
import { SessionExpiryRedirect } from './session-expiry-redirect';
import { ToastProvider } from '../shared/ui/toast';

export function AppProviders({ children }: { children: ReactNode }) {
  return (
    <CurrentUserProvider>
      <GuildProvider>
        <NotificationPrefsProvider>
          <ToastProvider>
            <SessionExpiryRedirect />
            {children}
          </ToastProvider>
        </NotificationPrefsProvider>
      </GuildProvider>
    </CurrentUserProvider>
  );
}

