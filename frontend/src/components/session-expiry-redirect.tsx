'use client';

import { useEffect } from 'react';
import { usePathname, useRouter } from 'next/navigation';
import { stopChatHub } from '../shared/api/chat-hub';
import { onSessionInvalidated } from '../shared/lib/session';

const PUBLIC_PATHS = new Set(['/']);
const AUTH_PATHS = new Set(['/auth/login', '/auth/register']);

function isProtectedPath(pathname: string | null): boolean {
  if (!pathname || pathname.startsWith('/_next') || pathname === '/favicon.ico') {
    return false;
  }

  if (PUBLIC_PATHS.has(pathname) || AUTH_PATHS.has(pathname)) {
    return false;
  }

  return !pathname.startsWith('/auth');
}

export function SessionExpiryRedirect() {
  const pathname = usePathname();
  const router = useRouter();

  useEffect(() => {
    return onSessionInvalidated(() => {
      stopChatHub();

      if (isProtectedPath(pathname)) {
        router.replace('/auth/login');
        return;
      }

      router.refresh();
    });
  }, [pathname, router]);

  return null;
}
