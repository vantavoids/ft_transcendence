'use client';

import { Dispatch, SetStateAction, useEffect, useState } from 'react';
import { hasDm, type DirectMessage } from '../../components/dm-list';
import { archiveDirectMessageConversation, listDirectMessages } from '../api/chat';
import { onChatHubEvent } from '../api/chat-hub';
import {
  accentForAuthor,
  authorLabel,
  formatMessageTimestamp,
  mapDirectMessageConversation,
  splitMessageLines
} from '../mappers/chat';

const LAST_CHAT_DM_KEY = 'ft_transcendence_last_chat_dm';

export type DmWorkspace = {
  dmConversations: DirectMessage[];
  setDmConversations: Dispatch<SetStateAction<DirectMessage[]>>;
  showArchivedDms: boolean;
  activeDm: string | null;
  selectDm: (dmId: string) => void;
  clearActiveDm: () => void;
  toggleShowArchivedDms: () => void;
  archiveDm: (dmId: string) => Promise<void>;
};

// owns the DM conversation list (incl. the archived-view toggle) and the
// active DM selection/restoration - message history lives in
// useConversationHistory since it's shared with channels.
export function useDmWorkspace(currentUserId: string | null): DmWorkspace {
  const [dmConversations, setDmConversations] = useState<DirectMessage[]>([]);
  const [showArchivedDms, setShowArchivedDms] = useState(false);
  const [activeDm, setActiveDm] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function loadDms() {
      try {
        const dtos = await listDirectMessages({ include_archived: showArchivedDms });
        if (cancelled) {
          return;
        }

        const mapped = dtos.map(mapDirectMessageConversation);
        setDmConversations(mapped);

        const storedDmId = window.sessionStorage.getItem(LAST_CHAT_DM_KEY);
        if (storedDmId && hasDm(storedDmId, mapped)) {
          setActiveDm(storedDmId);
        }
      } catch {
        // best effort: leave the DM list empty if the chat service is unreachable
      }
    }

    loadDms();

    return () => {
      cancelled = true;
    };
  }, [showArchivedDms]);

  useEffect(() => {
    return onChatHubEvent('DmReadStateUpdated', (event) => {
      setDmConversations((current) =>
        current.map((dm) =>
          dm.id === event.partner_id ? { ...dm, unreadCount: event.unread_count } : dm
        )
      );
    });
  }, []);

  // keep the sidebar preview/timestamp/ordering live - previously only
  // updated optimistically for messages *you* sent, never for incoming ones
  useEffect(() => {
    return onChatHubEvent('ReceiveDirectMessage', (event) => {
      const partnerId = event.sender_id === currentUserId ? event.recipient_id : event.sender_id;
      const preview = splitMessageLines(event.content ?? '')[0] ?? '';
      const lastMessageAt = formatMessageTimestamp(event.created_at);
      const lastActivityAt = Date.parse(event.created_at);

      setDmConversations((current) => {
        if (current.some((dm) => dm.id === partnerId)) {
          return current.map((dm) =>
            dm.id === partnerId
              ? { ...dm, lastMessage: preview, lastMessageAt, lastActivityAt, isArchived: false }
              : dm
          );
        }

        // first-ever message with this partner - the conversation didn't
        // exist in our list at all yet, so add it rather than wait for a refresh
        return [
          ...current,
          {
            id: partnerId,
            name: authorLabel(partnerId, currentUserId),
            status: 'offline',
            accent: accentForAuthor(partnerId),
            lastMessage: preview,
            lastMessageAt,
            lastActivityAt,
            unreadCount: 0,
            isArchived: false
          }
        ];
      });
    });
  }, [currentUserId]);

  function selectDm(dmId: string) {
    setActiveDm(dmId);
    window.sessionStorage.setItem(LAST_CHAT_DM_KEY, dmId);
  }

  function clearActiveDm() {
    setActiveDm(null);
  }

  function toggleShowArchivedDms() {
    setShowArchivedDms((current) => !current);
  }

  async function archiveDm(dmId: string) {
    try {
      await archiveDirectMessageConversation(dmId);

      if (showArchivedDms) {
        setDmConversations((current) =>
          current.map((dm) => (dm.id === dmId ? { ...dm, isArchived: true } : dm))
        );
      } else {
        setDmConversations((current) => current.filter((dm) => dm.id !== dmId));
      }

      setActiveDm((current) => (current === dmId ? null : current));
    } catch {
      // best effort: leave the list as-is, allow the user to retry
    }
  }

  return {
    dmConversations,
    setDmConversations,
    showArchivedDms,
    activeDm,
    selectDm,
    clearActiveDm,
    toggleShowArchivedDms,
    archiveDm
  };
}
