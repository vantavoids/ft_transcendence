'use client';

import { useEffect, useState } from 'react';
import { getIdentity } from '../api/auth';

export function useCurrentUserId(): string | null {
  const [currentUserId, setCurrentUserId] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    getIdentity()
      .then((identity) => {
        if (!cancelled) {
          setCurrentUserId(identity.id);
        }
      })
      .catch(() => {
        // best effort: message-ownership checks fall back to "not mine" until this resolves
      });

    return () => {
      cancelled = true;
    };
  }, []);

  return currentUserId;
}
