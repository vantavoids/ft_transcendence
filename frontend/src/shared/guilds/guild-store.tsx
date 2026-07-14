'use client';

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import {
  createGuild as requestCreateGuild,
  createGuildChannel,
  joinGuild,
  listMyGuilds,
  previewInvite,
  transferGuildOwnership,
  type CreateGuildPayload,
  type GuildDto,
  type MyGuildDto
} from '../api/guild';
import { ApiError } from '../api/client';
import { onChatHubEvent } from '../api/chat-hub';
import { getCurrentUser } from '../api/user';
import { getAccessToken, invalidateSession } from '../lib/session';

export const LAST_GUILD_KEY = 'ft_transcendence_last_guild';

type GuildStoreValue = {
  guilds: MyGuildDto[];
  isLoading: boolean;
  /** True once the guild list has been fetched successfully at least once. */
  hasLoadedGuilds: boolean;
  error: string | null;
  selectedGuildId: string | null;
  selectedGuild: MyGuildDto | null;
  currentUserId: string | null;
  selectGuild: (guildId: string) => void;
  refreshGuilds: () => Promise<void>;
  createGuild: (payload: CreateGuildPayload) => Promise<GuildDto>;
  joinGuildWithCode: (code: string) => Promise<GuildDto>;
  transferOwnership: (guildId: string, newOwnerId: string) => Promise<void>;
};

const GuildStoreContext = createContext<GuildStoreValue | null>(null);

function readStoredGuildId() {
  if (typeof window === 'undefined') {
    return null;
  }

  return window.sessionStorage.getItem(LAST_GUILD_KEY);
}

function persistGuildId(guildId: string | null) {
  if (typeof window === 'undefined') {
    return;
  }

  if (guildId) {
    window.sessionStorage.setItem(LAST_GUILD_KEY, guildId);
  } else {
    window.sessionStorage.removeItem(LAST_GUILD_KEY);
  }
}

function hasSession() {
  if (typeof window === 'undefined') {
    return false;
  }

  return Boolean(getAccessToken());
}

function sortByJoinedAt(guilds: MyGuildDto[]) {
  return [...guilds].sort(
    (a, b) => new Date(b.joined_at).getTime() - new Date(a.joined_at).getTime()
  );
}

export function GuildProvider({ children }: { children: ReactNode }) {
  const [guilds, setGuilds] = useState<MyGuildDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [hasLoadedGuilds, setHasLoadedGuilds] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedGuildId, setSelectedGuildId] = useState<string | null>(null);
  const [currentUserId, setCurrentUserId] = useState<string | null>(null);

  const applyGuilds = useCallback((nextGuilds: MyGuildDto[], preferredId?: string) => {
    const sorted = sortByJoinedAt(nextGuilds);
    setGuilds(sorted);
    setSelectedGuildId((current) => {
      const candidates = [preferredId, current, readStoredGuildId()];
      const next =
        candidates.find((id) => id && sorted.some((guild) => guild.id === id)) ??
        sorted[0]?.id ??
        null;

      persistGuildId(next);
      return next;
    });
  }, []);

  const refreshGuilds = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      applyGuilds(await listMyGuilds());
      setHasLoadedGuilds(true);
    } catch (refreshError) {
      setError(refreshError instanceof Error ? refreshError.message : 'Failed to load guilds.');
    } finally {
      setIsLoading(false);
    }
  }, [applyGuilds]);

  useEffect(() => {
    if (!hasSession()) {
      return;
    }

    void refreshGuilds();
    getCurrentUser()
      .then((user) => setCurrentUserId(user.id))
      .catch((error) => {
        if (error instanceof ApiError && error.status === 404) {
          invalidateSession();
        }
        setCurrentUserId(null);
      });
  }, [refreshGuilds]);

  const selectGuild = useCallback((guildId: string) => {
    setSelectedGuildId(guildId);
    persistGuildId(guildId);
  }, []);

  // live ownership changes (broadcast by Chat to the guild group): patch owner_id
  // in place so every owner-derived view (crown, management controls) flips for
  // both the old and new owner without a refresh. gated on a session so we never
  // open the hub before login.
  useEffect(() => {
    if (!currentUserId) {
      return;
    }

    return onChatHubEvent('GuildOwnerTransferred', (event) => {
      setGuilds((current) =>
        current.map((guild) =>
          guild.id === event.guild_id ? { ...guild, owner_id: event.new_owner_id } : guild
        )
      );
    });
  }, [currentUserId]);

  const createGuild = useCallback(
    async (payload: CreateGuildPayload) => {
      const guild = await requestCreateGuild(payload);

      try {
        await createGuildChannel(guild.id, { name: 'general', type: 'text' });
      } catch {
        // best effort: the guild still exists even if the default channel failed to create
      }

      try {
        applyGuilds(await listMyGuilds(), guild.id);
      } catch {
        // The guild exists even if the refresh failed; surface it optimistically.
        applyGuilds(
          [
            ...guilds,
            {
              id: guild.id,
              name: guild.name,
              icon_url: guild.icon_url,
              owner_id: guild.owner_id,
              member_count: guild.member_count ?? 1,
              joined_at: guild.created_at
            }
          ],
          guild.id
        );
      }

      return guild;
    },
    [applyGuilds, guilds]
  );

  const joinGuildWithCode = useCallback(
    async (code: string) => {
      const preview = await previewInvite(code.trim());
      const guild = await joinGuild(preview.guild.id, code.trim());
      applyGuilds(await listMyGuilds(), guild.id);
      return guild;
    },
    [applyGuilds]
  );

  // after handing ownership over, re-fetch the guild list so owner_id (and the
  // owner-only UI derived from it) reflects the new owner everywhere.
  const transferOwnership = useCallback(
    async (guildId: string, newOwnerId: string) => {
      await transferGuildOwnership(guildId, newOwnerId);
      await refreshGuilds();
    },
    [refreshGuilds]
  );

  const selectedGuild = useMemo(
    () => guilds.find((guild) => guild.id === selectedGuildId) ?? null,
    [guilds, selectedGuildId]
  );

  const value = useMemo(
    () => ({
      guilds,
      isLoading,
      hasLoadedGuilds,
      error,
      selectedGuildId,
      selectedGuild,
      currentUserId,
      selectGuild,
      refreshGuilds,
      createGuild,
      joinGuildWithCode,
      transferOwnership
    }),
    [
      guilds,
      isLoading,
      hasLoadedGuilds,
      error,
      selectedGuildId,
      selectedGuild,
      currentUserId,
      selectGuild,
      refreshGuilds,
      createGuild,
      joinGuildWithCode,
      transferOwnership
    ]
  );

  return <GuildStoreContext.Provider value={value}>{children}</GuildStoreContext.Provider>;
}

export function useGuilds() {
  const context = useContext(GuildStoreContext);

  if (!context) {
    throw new Error('useGuilds must be used within a GuildProvider.');
  }

  return context;
}
