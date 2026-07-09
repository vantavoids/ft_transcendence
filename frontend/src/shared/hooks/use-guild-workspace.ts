'use client';

import { useEffect, useState } from 'react';
import { hasChannel, type ChannelCategory, type TextChannel } from '../../components/channel-list';
import type { Guild } from '../../components/guild-sidebar';
import {
  listGuildCategories,
  listGuildChannels,
  listMyGuilds,
  type GuildCategoryDto,
  type GuildChannelDto,
  type MyGuildDto
} from '../api/guild';
import { listChannelReadStates } from '../api/chat';
import { joinChatChannel, leaveChatChannel, onChatHubEvent, onChatHubReconnected } from '../api/chat-hub';

const LAST_CHAT_GUILD_KEY = 'ft_transcendence_last_chat_guild';
const LAST_CHAT_CHANNEL_KEY = 'ft_transcendence_last_chat_channel';
const UNCATEGORIZED_CATEGORY_ID = 'uncategorized';

function guildIconUrl(iconUrl: string | null | undefined, name: string) {
  if (iconUrl) {
    return iconUrl;
  }

  const initial = encodeURIComponent(name.slice(0, 1).toUpperCase() || 'G');
  return `https://placehold.co/160x160/23232b/e8e8ec/png?text=${initial}`;
}

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
  guilds: Guild[];
  activeGuildId: string | null;
  channelCategories: ChannelCategory[];
  channels: TextChannel[];
  activeChannel: string | null;
  channelReadStates: Record<string, ChannelReadState>;
  selectGuild: (guildId: string) => void;
  selectChannel: (channelId: string) => void;
  markChannelReadLocally: (channelId: string, messageId: string) => void;
};

// owns everything needed to render the guild sidebar + channel list: the
// guild list, the active guild's channels/categories, and last-viewed
// restoration - callers only handle cross-cutting concerns (chat mode,
// mobile pane, scroll memory) around selectGuild/selectChannel.
export function useGuildWorkspace(): GuildWorkspace {
  const [guilds, setGuilds] = useState<Guild[]>([]);
  const [activeGuildId, setActiveGuildId] = useState<string | null>(null);
  const [channelCategories, setChannelCategories] = useState<ChannelCategory[]>([]);
  const [channels, setChannels] = useState<TextChannel[]>([]);
  const [activeChannel, setActiveChannel] = useState<string | null>(null);
  const [channelReadStates, setChannelReadStates] = useState<Record<string, ChannelReadState>>({});

  useEffect(() => {
    let cancelled = false;

    async function loadGuilds() {
      try {
        const dtos = await listMyGuilds();
        if (cancelled) {
          return;
        }

        const sorted = [...dtos].sort((a, b) => b.joined_at.localeCompare(a.joined_at));
        setGuilds(
          sorted.map((guild) => ({
            id: guild.id,
            name: guild.name,
            iconUrl: guildIconUrl(guild.icon_url, guild.name)
          }))
        );

        const storedGuildId = window.sessionStorage.getItem(LAST_CHAT_GUILD_KEY);
        const initialGuildId = sorted.some((guild) => guild.id === storedGuildId)
          ? storedGuildId
          : (sorted[0]?.id ?? null);
        setActiveGuildId(initialGuildId);
      } catch {
        // best effort: leave the sidebar empty if the guild service is unreachable
      }
    }

    loadGuilds();

    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    const unsubscribeJoined = onChatHubEvent('GuildJoined', (event) => {
      setGuilds((current) =>
        current.some((guild) => guild.id === event.guild_id)
          ? current
          : [
              ...current,
              {
                id: event.guild_id,
                name: event.guild_name,
                iconUrl: guildIconUrl(null, event.guild_name)
              }
            ]
      );
    });

    const unsubscribeLeft = onChatHubEvent('GuildLeft', (event) => {
      setGuilds((current) => current.filter((guild) => guild.id !== event.guild_id));
      setActiveGuildId((current) => (current === event.guild_id ? null : current));
    });

    return () => {
      unsubscribeJoined();
      unsubscribeLeft();
    };
  }, []);

  useEffect(() => {
    if (!activeGuildId) {
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

    loadChannels(activeGuildId);

    return () => {
      cancelled = true;
    };
  }, [activeGuildId]);

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

  // join/leave the hub group for whichever channel is active.
  useEffect(() => {
    if (!activeChannel) {
      return;
    }

    const channelId = activeChannel;
    joinChatChannel(channelId).catch(() => {});
    // best effort: real-time updates for this channel just won't arrive

    const unsubscribeReconnect = onChatHubReconnected(() => {
      joinChatChannel(channelId).catch(() => {});
    });

    return () => {
      unsubscribeReconnect();
      leaveChatChannel(channelId).catch(() => {});
    };
  }, [activeChannel]);

  useEffect(() => {
    return onChatHubEvent('ReadStateUpdated', (event) => {
      setChannelReadStates((current) => ({
        ...current,
        [event.channel_id]: {
          lastReadMessageId: event.last_read_message_id,
          unreadCount: event.unread_count
        }
      }));
    });
  }, []);

  function markChannelReadLocally(channelId: string, messageId: string) {
    setChannelReadStates((current) => ({
      ...current,
      [channelId]: { lastReadMessageId: messageId, unreadCount: 0 }
    }));
  }

  function selectGuild(guildId: string) {
    setActiveGuildId(guildId);
    window.sessionStorage.setItem(LAST_CHAT_GUILD_KEY, guildId);
  }

  function selectChannel(channelId: string) {
    setActiveChannel(channelId);
    window.sessionStorage.setItem(LAST_CHAT_CHANNEL_KEY, channelId);
  }

  return {
    guilds,
    activeGuildId,
    channelCategories,
    channels,
    activeChannel,
    channelReadStates,
    selectGuild,
    selectChannel,
    markChannelReadLocally
  };
}
