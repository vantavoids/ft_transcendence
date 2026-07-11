// Client-side display preferences, persisted in localStorage. Same-tab
// consumers get change notifications through a window event (the 'storage'
// event only fires in other tabs).

export const GROUP_MEMBERS_BY_ROLE_KEY = 'ft_transcendence_group_members_by_role';
const PREFERENCES_CHANGED_EVENT = 'ft_transcendence:preferences-changed';

export function getGroupMembersByRole(): boolean {
  if (typeof window === 'undefined') {
    return true;
  }

  return window.localStorage.getItem(GROUP_MEMBERS_BY_ROLE_KEY) !== 'false';
}

export function setGroupMembersByRole(enabled: boolean): void {
  if (typeof window === 'undefined') {
    return;
  }

  window.localStorage.setItem(GROUP_MEMBERS_BY_ROLE_KEY, String(enabled));
  window.dispatchEvent(new Event(PREFERENCES_CHANGED_EVENT));
}

export function subscribePreferences(handler: () => void): () => void {
  if (typeof window === 'undefined') {
    return () => {};
  }

  window.addEventListener(PREFERENCES_CHANGED_EVENT, handler);
  window.addEventListener('storage', handler);
  return () => {
    window.removeEventListener(PREFERENCES_CHANGED_EVENT, handler);
    window.removeEventListener('storage', handler);
  };
}
