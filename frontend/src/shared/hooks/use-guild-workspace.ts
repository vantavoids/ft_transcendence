'use client';

import { useEffect, useMemo, useRef, useState } from 'react';
import { hasChannel, type ChannelCategory, type TextChannel } from '../../components/channel-list';
import {
  listGuildCategories,
  listGuildChannels,
  type GuildCategoryDto,
  type GuildChannelDto
} from '../api/guild';
import { listChannelReadStates } from '../api/chat';
import { joinChatChannel, leaveChatChannel, onChatHubEvent, onChatHubReconnected } from '../api/chat-hub';
import { useGuilds } from '../guilds/guild-store';

const LAST_CHAT_CHANNEL_KEY = 'ft_transcendence_last_chat_channel';
const UNCATEGORIZED_CATEGORY_ID = 'uncategorized';

function buildChannelCategories(
  channels: GuildChannelDto[],
  categories: GuildCategoryDto[]
): ChannelCategory[] {
  const sortedCategories = [...categories].sort((a, b) => a.position - b.position);

  const grouped: ChannelCategory[] = sortedCategories.map((category) => ({
    id: category.id,
    name: category.name,
    channels: channels
      .filter((channel) => channel.category_id === category.id)
      .sort((a, b) => a.position - b.position)
      .map((channel) => ({ id: channel.id, name: channel.name }))
  }));

  const uncategorized = channels
    .filter((channel) => !channel.category_id)
    .sort((a, b) => a.position - b.position)
    .map((channel) => ({ id: channel.id, name: channel.name }));

  if (uncategorized.length > 0) {
    grouped.push({ id: UNCATEGORIZED_CATEGORY_ID, name: 'Channels', channels: uncategorized });
  }

  return grouped;
}

export type ChannelReadState = {
  lastReadMessageId: string | null;
  unreadCount: number;
};

export type GuildWorkspace = {
  channelCategories: ChannelCategory[];
  channels: TextChannel[];
  activeChannel: string | null;
  channelReadStates: Record<string, ChannelReadState>;
  selectChannel: (channelId: string) => void;
  markChannelReadLocally: (channelId: string, messageId: string) => void;
  refreshChannels: () => void;
};

// owns the active guild's channels/categories, read states, and the SignalR
// channel-join-all-for-unread-counts wiring - the guild list itself (which
// guild is selected, joining/creating/leaving) lives in the app-wide
// GuildProvider (shared/guilds/guild-store.tsx) since it's also needed by the
// /guilds management pages, not just chat.
export function useGuildWorkspace(): GuildWorkspace {
  const { selectedGuildId, refreshGuilds, currentUserId } = useGuilds();
  // raw server DTOs are kept so real-time channel events can be spliced in
  // without a refetch; the sidebar-shaped views below are derived from them.
  const [rawChannels, setRawChannels] = useState<GuildChannelDto[]>([]);
  const [rawCategories, setRawCategories] = useState<GuildCategoryDto[]>([]);
  const [activeChannel, setActiveChannel] = useState<string | null>(null);
  const [channelReadStates, setChannelReadStates] = useState<Record<string, ChannelReadState>>({});
  const activeChannelRef = useRef<string | null>(null);
  activeChannelRef.current = activeChannel;
  const selectedGuildIdRef = useRef<string | null>(selectedGuildId);
  selectedGuildIdRef.current = selectedGuildId;
  const [channelsRefreshKey, setChannelsRefreshKey] = useState(0);

  const channelCategories = useMemo(
    () => buildChannelCategories(rawChannels, rawCategories),
    [rawChannels, rawCategories]
  );
  const channels = useMemo<TextChannel[]>(
    () => rawChannels.map((channel) => ({ id: channel.id, name: channel.name })),
    [rawChannels]
  );
  // stable identity of the channel set: the join/leave-all effect keys off this
  // so a rename (same ids) doesn't tear down and re-join every SignalR group.
  const channelIdsKey = useMemo(() => channels.map((channel) => channel.id).join(','), [channels]);

  useEffect(() => {
    const unsubscribeJoined = onChatHubEvent('GuildJoined', (event) => {
      void refreshGuilds();
      // if the just-joined guild is the one on screen, its channel load may have
      // raced the membership commit and 403'd; re-load now that we are a member.
      if (event.guild_id === selectedGuildIdRef.current) {
        setChannelsRefreshKey((key) => key + 1);
      }
    });

    const unsubscribeLeft = onChatHubEvent('GuildLeft', () => {
      void refreshGuilds();
    });

    // a deleted guild is dropped from listMyGuilds, so refreshing the list also
    // reconciles the selection (applyGuilds falls back to another guild/none)
    // and clears the now-gone guild's channels.
    const unsubscribeDeleted = onChatHubEvent('GuildDeleted', () => {
      void refreshGuilds();
    });

    // name/icon change: refresh the guild list so the sidebar + header pick up
    // the new values.
    const unsubscribeUpdated = onChatHubEvent('GuildUpdated', () => {
      void refreshGuilds();
    });

    return () => {
      unsubscribeJoined();
      unsubscribeLeft();
      unsubscribeDeleted();
      unsubscribeUpdated();
    };
  }, [refreshGuilds]);

  useEffect(() => {
    if (!selectedGuildId) {
      setRawChannels([]);
      setRawCategories([]);
      return;
    }

    let cancelled = false;

    async function loadChannels(guildId: string) {
      try {
        const [channelDtos, categoryDtos] = await Promise.all([
          listGuildChannels(guildId),
          listGuildCategories(guildId)
        ]);
        if (cancelled) {
          return;
        }

        setRawChannels(channelDtos);
        setRawCategories(categoryDtos);
      } catch {
        if (!cancelled) {
          setRawChannels([]);
          setRawCategories([]);
        }
      }
    }

    loadChannels(selectedGuildId);

    return () => {
      cancelled = true;
    };
  }, [selectedGuildId, channelsRefreshKey]);

  // keep the active channel valid as the channel set changes (initial load, a
  // live create/delete). when the open channel is removed, fall back to the
  // last-used or first available channel.
  useEffect(() => {
    setActiveChannel((current) => {
      if (current && hasChannel(current, channels)) {
        return current;
      }
      const storedChannelId = window.sessionStorage.getItem(LAST_CHAT_CHANNEL_KEY);
      return storedChannelId && hasChannel(storedChannelId, channels)
        ? storedChannelId
        : (channels[0]?.id ?? null);
    });
  }, [channels]);

  // splice live channel lifecycle events into the raw set. Guild targets these
  // only at members who may read the channel, so anything that arrives is
  // something the current user is allowed to see.
  useEffect(() => {
    const upsert = (channel: GuildChannelDto) => {
      if (channel.guild_id !== selectedGuildIdRef.current) {
        return;
      }
      setRawChannels((current) => {
        const index = current.findIndex((existing) => existing.id === channel.id);
        if (index === -1) {
          return [...current, channel];
        }
        const next = [...current];
        next[index] = channel;
        return next;
      });
    };

    const unsubscribeCreated = onChatHubEvent('ChannelCreated', upsert);
    const unsubscribeUpdated = onChatHubEvent('ChannelUpdated', upsert);
    const unsubscribeDeleted = onChatHubEvent('ChannelDeleted', (event) => {
      if (event.guild_id !== selectedGuildIdRef.current) {
        return;
      }
      setRawChannels((current) => current.filter((channel) => channel.id !== event.channel_id));
    });

    const upsertCategory = (category: GuildCategoryDto) => {
      if (category.guild_id !== selectedGuildIdRef.current) {
        return;
      }
      setRawCategories((current) => {
        const index = current.findIndex((existing) => existing.id === category.id);
        if (index === -1) {
          return [...current, category];
        }
        const next = [...current];
        next[index] = category;
        return next;
      });
    };

    const unsubscribeCategoryCreated = onChatHubEvent('CategoryCreated', upsertCategory);
    const unsubscribeCategoryUpdated = onChatHubEvent('CategoryUpdated', upsertCategory);
    const unsubscribeCategoryDeleted = onChatHubEvent('CategoryDeleted', (event) => {
      if (event.guild_id !== selectedGuildIdRef.current) {
        return;
      }
      setRawCategories((current) => current.filter((category) => category.id !== event.category_id));
    });

    return () => {
      unsubscribeCreated();
      unsubscribeUpdated();
      unsubscribeDeleted();
      unsubscribeCategoryCreated();
      unsubscribeCategoryUpdated();
      unsubscribeCategoryDeleted();
    };
  }, []);

  useEffect(() => {
    let cancelled = false;

    listChannelReadStates()
      .then((dtos) => {
        if (cancelled) {
          return;
        }

        const states: Record<string, ChannelReadState> = {};
        for (const dto of dtos) {
          states[dto.channel_id] = {
            lastReadMessageId: dto.last_read_message_id,
            unreadCount: dto.unread_count
          };
        }
        setChannelReadStates(states);
      })
      .catch(() => {
        // best effort: sidebar just shows no unread badges until this resolves
      });

    return () => {
      cancelled = true;
    };
  }, []);

  // join every channel of the active guild (not just the one being viewed),
  // so unread badges for other channels update live too. keyed off the stable
  // id set so a live rename (same ids) does not re-join everything.
  useEffect(() => {
    if (!channelIdsKey) {
      return;
    }

    const channelIds = channelIdsKey.split(',');

    function joinAll() {
      for (const channelId of channelIds) {
        joinChatChannel(channelId).catch(() => {
          // best effort: real-time updates for this channel just won't arrive
        });
      }
    }

    joinAll();

    const unsubscribeReconnect = onChatHubReconnected(joinAll);

    return () => {
      unsubscribeReconnect();
      for (const channelId of channelIds) {
        leaveChatChannel(channelId).catch(() => {});
      }
    };
  }, [channelIdsKey]);

  useEffect(() => {
    const unsubscribeReadState = onChatHubEvent('ReadStateUpdated', (event) => {
      setChannelReadStates((current) => ({
        ...current,
        [event.channel_id]: {
          lastReadMessageId: event.last_read_message_id,
          unreadCount: event.unread_count
        }
      }));
    });

    // ReadStateUpdated only self-syncs when *you* mark something read - it
    // never fires just because a new message arrived, so update unread counts
    // locally on new messages. This fires for all active guild channels, so
    // skip the channel the user currently has open.
    const unsubscribeReceiveMessage = onChatHubEvent('ReceiveMessage', (event) => {
      if (event.channel_id === activeChannelRef.current) {
        return;
      }
      setChannelReadStates((current) => {
        const existing = current[event.channel_id];
        return {
          ...current,
          [event.channel_id]: {
            lastReadMessageId: existing?.lastReadMessageId ?? null,
            unreadCount: (existing?.unreadCount ?? 0) + 1
          }
        };
      });
    });

    return () => {
      unsubscribeReadState();
      unsubscribeReceiveMessage();
    };
  }, [currentUserId]);

  function markChannelReadLocally(channelId: string, messageId: string) {
    setChannelReadStates((current) => ({
      ...current,
      [channelId]: { lastReadMessageId: messageId, unreadCount: 0 }
    }));
  }

  function selectChannel(channelId: string) {
    setActiveChannel(channelId);
    window.sessionStorage.setItem(LAST_CHAT_CHANNEL_KEY, channelId);
  }

  function refreshChannels() {
    setChannelsRefreshKey((key) => key + 1);
  }

  return {
    channelCategories,
    channels,
    activeChannel,
    channelReadStates,
    selectChannel,
    markChannelReadLocally,
    refreshChannels
  };
}
