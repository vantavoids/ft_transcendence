const FRIENDS_CHANGED_EVENT = 'ft-transcendence:friends-changed';

// fired when the current user's friend graph changed elsewhere (e.g. a pending
// request they sent was accepted, surfaced via a friend_accept notification),
// so the friends list can re-fetch without a manual refresh.
export function dispatchFriendsChanged(): void {
  if (typeof window === 'undefined') {
    return;
  }

  window.dispatchEvent(new CustomEvent(FRIENDS_CHANGED_EVENT));
}

export function subscribeFriendsChanged(handler: () => void): () => void {
  if (typeof window === 'undefined') {
    return () => undefined;
  }

  window.addEventListener(FRIENDS_CHANGED_EVENT, handler);

  return () => {
    window.removeEventListener(FRIENDS_CHANGED_EVENT, handler);
  };
}
