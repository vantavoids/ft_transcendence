'use client';

import { Dispatch, SetStateAction, useCallback, useEffect, useRef, useState } from 'react';
import { hasDm, type DirectMessage } from '../../components/dm-list';
import { archiveDirectMessageConversation, listDirectMessages } from '../api/chat';
import { getUsersByIds } from '../api/user';
import { onChatHubEvent } from '../api/chat-hub';
import { subscribeProfileUpdated } from '../lib/profile-events';
import { accentForId } from '../lib/accent';
import {
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
  openDmWith: (profile: {
    id: string;
    name: string;
    status: DirectMessage['status'];
    accent: DirectMessage['accent'];
    avatarUrl?: string | null;
    bannerUrl?: string | null;
    bio?: string | null;
  }) => void;
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
  const activeDmRef = useRef<string | null>(null);
  activeDmRef.current = activeDm;

  const loadDms = useCallback(async () => {
    try {
      const dtos = await listDirectMessages({ include_archived: showArchivedDms });
      const users = await getUsersByIds(dtos.map((dto) => dto.partner_id));

      const usersById = new Map(users.map((user) => [user.id, user]));
      const mapped = dtos.map((dto) => {
        const base = mapDirectMessageConversation(dto);
        const user = usersById.get(dto.partner_id);

        if (!user) {
          return base;
        }

        return {
          ...base,
          name: user.display_name || user.username,
          status: user.status === 'dnd' ? 'idle' : user.status,
          avatarUrl: user.avatar_url ?? null,
          bannerUrl: user.banner_url ?? null,
          bio: user.bio ?? null
        };
      });
      setDmConversations(mapped);

      const storedDmId = window.sessionStorage.getItem(LAST_CHAT_DM_KEY);
      if (storedDmId && hasDm(storedDmId, mapped)) {
        setActiveDm(storedDmId);
      }
    } catch {
      // best effort: leave the DM list empty if the chat service is unreachable
    }
  }, [showArchivedDms]);

  useEffect(() => {
    void loadDms();
  }, [loadDms]);

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
      const isIncoming = event.recipient_id === currentUserId;
      const preview = splitMessageLines(event.content ?? '')[0] ?? '';
      const lastMessageAt = formatMessageTimestamp(event.created_at);
      const lastActivityAt = Date.parse(event.created_at);

      setDmConversations((current) => {
        if (current.some((dm) => dm.id === partnerId)) {
          return current.map((dm) =>
            dm.id === partnerId
              ? {
                  ...dm,
                  lastMessage: preview,
                  lastMessageAt,
                  lastActivityAt,
                  isArchived: false,
                  unreadCount:
                    isIncoming && partnerId !== activeDmRef.current
                      ? dm.unreadCount + 1
                      : dm.unreadCount
                }
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
            accent: accentForId(partnerId),
            lastMessage: preview,
            lastMessageAt,
            lastActivityAt,
            unreadCount: isIncoming && partnerId !== activeDmRef.current ? 1 : 0,
            isArchived: false,
            avatarUrl: null,
            bannerUrl: null,
            bio: null
          }
        ];
      });
    });
  }, [currentUserId]);

  useEffect(() => {
    return subscribeProfileUpdated(() => {
      void loadDms();
    });
  }, [loadDms]);

  function selectDm(dmId: string) {
    setActiveDm(dmId);
    window.sessionStorage.setItem(LAST_CHAT_DM_KEY, dmId);
  }

  // opens a DM with a user who may not have an existing conversation yet -
  // seeds an empty placeholder entry (mirrors the ReceiveDirectMessage
  // first-message case above) so the composer isn't gated on dmConversations
  // already containing this partner.
  function openDmWith(profile: {
    id: string;
    name: string;
    status: DirectMessage['status'];
    accent: DirectMessage['accent'];
    avatarUrl?: string | null;
    bannerUrl?: string | null;
    bio?: string | null;
  }) {
    setDmConversations((current) => {
      if (current.some((dm) => dm.id === profile.id)) {
        return current;
      }

      return [
        ...current,
        {
          id: profile.id,
          name: profile.name,
          status: profile.status,
          accent: profile.accent,
          lastMessage: '',
          lastMessageAt: '',
          lastActivityAt: Date.now(),
          unreadCount: 0,
          isArchived: false,
          avatarUrl: profile.avatarUrl ?? null,
          bannerUrl: profile.bannerUrl ?? null,
          bio: profile.bio ?? null
        }
      ];
    });

    selectDm(profile.id);
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
    openDmWith,
    clearActiveDm,
    toggleShowArchivedDms,
    archiveDm
  };
}
