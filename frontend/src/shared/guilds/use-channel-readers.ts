'use client';

import { useCallback, useEffect, useState } from 'react';
import { listChannelReaders } from '../api/guild';
import { onChatHubEvent } from '../api/chat-hub';

// Set of member ids that can READ the given channel, or null when unknown (no
// channel selected, still loading, or the lookup failed). A null result means
// "don't filter" so a transient error never blanks the whole member list.
export function useChannelReaders(guildId: string | null, channelId: string | null) {
  const [readerIds, setReaderIds] = useState<Set<string> | null>(null);

  const load = useCallback(async () => {
    if (!guildId || !channelId) {
      setReaderIds(null);
      return;
    }

    try {
      const ids = await listChannelReaders(guildId, channelId);
      setReaderIds(new Set(ids));
    } catch {
      setReaderIds(null);
    }
  }, [guildId, channelId]);

  useEffect(() => {
    void load();
  }, [load]);

  // refetch when guild membership, roles, or the channel's permissions change,
  // so the list tracks who can see the channel without a manual refresh.
  useEffect(() => {
    if (!guildId || !channelId) {
      return;
    }

    const reloadIfThisGuild = (event: { guild_id: string }) => {
      if (event.guild_id === guildId) {
        void load();
      }
    };
    const reloadIfThisChannel = (event: { channel_id: string }) => {
      if (event.channel_id === channelId) {
        void load();
      }
    };

    const unsubscribers = [
      onChatHubEvent('MemberJoined', reloadIfThisGuild),
      onChatHubEvent('MemberLeft', reloadIfThisGuild),
      onChatHubEvent('MemberUpdated', reloadIfThisGuild),
      onChatHubEvent('RolesChanged', (changedGuildId) => {
        if (changedGuildId === guildId) {
          void load();
        }
      }),
      onChatHubEvent('ChannelUpdated', (channel) => {
        if (channel.id === channelId) {
          void load();
        }
      }),
      onChatHubEvent('ChannelAccessGranted', reloadIfThisChannel)
    ];

    return () => {
      for (const unsubscribe of unsubscribers) {
        unsubscribe();
      }
    };
  }, [guildId, channelId, load]);

  return readerIds;
}
