'use client';

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import {
  getCurrentUser,
  updateUserProfile,
  uploadAvatar,
  removeAvatar,
  uploadBanner,
  removeBanner
} from '../api/user';
import { getAccessToken } from '../lib/session';
import { toCurrentUserProfile, type CurrentUserProfile } from '../mappers/user';
import { validateProfileImageFile, validateProfileUpdateInput } from '../lib/validators/profile';

type CurrentUserContextValue = {
  currentUser: CurrentUserProfile | null;
  isLoading: boolean;
  error: string | null;
  refreshCurrentUser: () => Promise<void>;
  setCurrentUser: (user: CurrentUserProfile | null) => void;
  updateCurrentUserProfile: (payload: Parameters<typeof updateUserProfile>[1]) => Promise<void>;
  uploadCurrentUserAvatar: (file: File) => Promise<void>;
  removeCurrentUserAvatar: () => Promise<void>;
  uploadCurrentUserBanner: (file: File) => Promise<void>;
  removeCurrentUserBanner: () => Promise<void>;
};

const CurrentUserContext = createContext<CurrentUserContextValue | null>(null);

export function CurrentUserProvider({ children }: { children: ReactNode }) {
  const [currentUser, setCurrentUser] = useState<CurrentUserProfile | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const refreshCurrentUser = useCallback(async () => {
    if (!getAccessToken()) {
      setCurrentUser(null);
      setError(null);
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const user = await getCurrentUser();
      setCurrentUser(toCurrentUserProfile(user));
    } catch (loadError) {
      setCurrentUser(null);
      setError(loadError instanceof Error ? loadError.message : 'Failed to load profile.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void refreshCurrentUser();
  }, [refreshCurrentUser]);

  const updateCurrentUserProfile = useCallback(
    async (payload: Parameters<typeof updateUserProfile>[1]) => {
      if (!currentUser) {
        return;
      }

      const validated = validateProfileUpdateInput(payload);
      const updated = await updateUserProfile(currentUser.id, validated);
      setCurrentUser(toCurrentUserProfile(updated));
    },
    [currentUser]
  );

  const uploadCurrentUserAvatar = useCallback(
    async (file: File) => {
      if (!currentUser) {
        return;
      }

      validateProfileImageFile(file, 'avatar');
      const updated = await uploadAvatar(currentUser.id, file);
      setCurrentUser((user) => (user ? { ...user, avatarUrl: updated.avatar_url } : user));
    },
    [currentUser]
  );

  const removeCurrentUserAvatar = useCallback(async () => {
    if (!currentUser) {
      return;
    }

    await removeAvatar(currentUser.id);
    setCurrentUser((user) => (user ? { ...user, avatarUrl: null } : user));
  }, [currentUser]);

  const uploadCurrentUserBanner = useCallback(
    async (file: File) => {
      if (!currentUser) {
        return;
      }

      validateProfileImageFile(file, 'banner');
      const updated = await uploadBanner(currentUser.id, file);
      setCurrentUser((user) => (user ? { ...user, bannerUrl: updated.banner_url } : user));
    },
    [currentUser]
  );

  const removeCurrentUserBanner = useCallback(async () => {
    if (!currentUser) {
      return;
    }

    await removeBanner(currentUser.id);
    setCurrentUser((user) => (user ? { ...user, bannerUrl: null } : user));
  }, [currentUser]);

  const value = useMemo(
    () => ({
      currentUser,
      isLoading,
      error,
      refreshCurrentUser,
      setCurrentUser,
      updateCurrentUserProfile,
      uploadCurrentUserAvatar,
      removeCurrentUserAvatar,
      uploadCurrentUserBanner,
      removeCurrentUserBanner
    }),
    [
      currentUser,
      isLoading,
      error,
      refreshCurrentUser,
      setCurrentUser,
      updateCurrentUserProfile,
      uploadCurrentUserAvatar,
      removeCurrentUserAvatar,
      uploadCurrentUserBanner,
      removeCurrentUserBanner
    ]
  );

  return <CurrentUserContext.Provider value={value}>{children}</CurrentUserContext.Provider>;
}

export function useCurrentUserProfile() {
  const context = useContext(CurrentUserContext);

  if (!context) {
    throw new Error('useCurrentUserProfile must be used within a CurrentUserProvider.');
  }

  return context;
}
