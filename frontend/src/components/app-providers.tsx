'use client';

import type { ReactNode } from 'react';
import { GuildProvider } from '../shared/guilds/guild-store';
import { CurrentUserProvider } from '../shared/user/user-store';
import { SessionExpiryRedirect } from './session-expiry-redirect';
import { ToastProvider } from '../shared/ui/toast';

export function AppProviders({ children }: { children: ReactNode }) {
  return (
    <CurrentUserProvider>
      <GuildProvider>
        <ToastProvider>
          <SessionExpiryRedirect />
          {children}
        </ToastProvider>
      </GuildProvider>
    </CurrentUserProvider>
  );
}

