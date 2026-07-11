'use client';

import { useSyncExternalStore } from 'react';
import { getGroupMembersByRole, subscribePreferences } from '../lib/preferences';

export function useGroupMembersByRole() {
  return useSyncExternalStore(subscribePreferences, getGroupMembersByRole, () => true);
}
