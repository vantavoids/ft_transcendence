'use client';

import { useEffect, useState } from 'react';
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
};

// owns the active guild's channels/categories, read states, and the SignalR
// channel-join-all-for-unread-counts wiring - the guild list itself (which
// guild is selected, joining/creating/leaving) lives in the app-wide
// GuildProvider (shared/guilds/guild-store.tsx) since it's also needed by the
// /guilds management pages, not just chat.
export function useGuildWorkspace(): GuildWorkspace {
  const { selectedGuildId, refreshGuilds } = useGuilds();
  const [channelCategories, setChannelCategories] = useState<ChannelCategory[]>([]);
  const [channels, setChannels] = useState<TextChannel[]>([]);
  const [activeChannel, setActiveChannel] = useState<string | null>(null);
  const [channelReadStates, setChannelReadStates] = useState<Record<string, ChannelReadState>>({});

  useEffect(() => {
    const unsubscribeJoined = onChatHubEvent('GuildJoined', () => {
      void refreshGuilds();
    });

    const unsubscribeLeft = onChatHubEvent('GuildLeft', () => {
      void refreshGuilds();
    });

    return () => {
      unsubscribeJoined();
      unsubscribeLeft();
    };
  }, [refreshGuilds]);

  useEffect(() => {
    if (!selectedGuildId) {
      setChannelCategories([]);
      setChannels([]);
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

        const flatChannels: TextChannel[] = channelDtos.map((channel) => ({
          id: channel.id,
          name: channel.name
        }));
        setChannelCategories(buildChannelCategories(channelDtos, categoryDtos));
        setChannels(flatChannels);

        const storedChannelId = window.sessionStorage.getItem(LAST_CHAT_CHANNEL_KEY);
        const initialChannelId =
          storedChannelId && hasChannel(storedChannelId, flatChannels)
            ? storedChannelId
            : (flatChannels[0]?.id ?? null);
        setActiveChannel(initialChannelId);
      } catch {
        if (!cancelled) {
          setChannelCategories([]);
          setChannels([]);
        }
      }
    }

    loadChannels(selectedGuildId);

    return () => {
      cancelled = true;
    };
  }, [selectedGuildId]);

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
  // so unread badges for other channels update live too.
  useEffect(() => {
    if (channels.length === 0) {
      return;
    }

    const channelIds = channels.map((channel) => channel.id);

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
  }, [channels]);

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
    // locally on new messages. This fires for all active guild channels.
    const unsubscribeReceiveMessage = onChatHubEvent('ReceiveMessage', (event) => {
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
  }, []);

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

  return {
    channelCategories,
    channels,
    activeChannel,
    channelReadStates,
    selectChannel,
    markChannelReadLocally
  };
}
