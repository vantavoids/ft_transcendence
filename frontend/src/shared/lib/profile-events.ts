const PROFILE_UPDATED_EVENT = 'ft-transcendence:profile-updated';

export type ProfileUpdatedDetail = {
  userId: string;
};

export function dispatchProfileUpdated(userId: string): void {
  if (typeof window === 'undefined') {
    return;
  }

  window.dispatchEvent(
    new CustomEvent<ProfileUpdatedDetail>(PROFILE_UPDATED_EVENT, {
      detail: { userId }
    })
  );
}

export function subscribeProfileUpdated(
  handler: (detail: ProfileUpdatedDetail) => void
): () => void {
  if (typeof window === 'undefined') {
    return () => undefined;
  }

  const listener = (event: Event) => {
    const customEvent = event as CustomEvent<ProfileUpdatedDetail>;
    handler(customEvent.detail);
  };

  window.addEventListener(PROFILE_UPDATED_EVENT, listener);

  return () => {
    window.removeEventListener(PROFILE_UPDATED_EVENT, listener);
  };
}
