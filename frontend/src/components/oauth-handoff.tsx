'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { establishSession } from '../shared/lib/session';

export function OAuthHandoff() {
  const router = useRouter();
  const [pending, setPending] = useState(false);

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const accessToken = params.get('access_token');
    if (!accessToken) {
      return;
    }

    setPending(true);
    establishSession(accessToken);
    window.history.replaceState(null, '', window.location.pathname);
    router.replace('/chat');
  }, [router]);

  if (!pending) {
    return null;
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-primary-bg">
      <p className="text-sm text-white/60">Signing you in...</p>
    </div>
  );
}
