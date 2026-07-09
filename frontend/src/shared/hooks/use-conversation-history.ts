'use client';

import { useEffect, useRef, useState } from 'react';
import type { ChatMessageData } from '../../components/chat-message';
import {
  deleteMessage as deleteMessageApi,
  editMessage as editMessageApi,
  listChannelMessages,
  listDirectMessageHistory,
  sendChannelMessage,
  sendDirectMessage
} from '../api/chat';
import { ApiError } from '../api/client';
import {
  accentForAuthor,
  authorLabel,
  formatMessageTimestamp,
  mapChannelMessage,
  mapDirectMessage,
  splitMessageLines
} from '../mappers/chat';

const MESSAGE_HISTORY_PAGE_SIZE = 50;

export type ConversationMode = 'guild' | 'dm';

export type ConversationHistory = {
  messagesByConversation: Record<string, ChatMessageData[]>;
  setMessagesByConversation: React.Dispatch<
    React.SetStateAction<Record<string, ChatMessageData[]>>
  >;
  loadOlderChannelHistory: (channelId: string) => Promise<void>;
  sendMessage: (content: string) => Promise<void>;
  updateMessage: (messageId: string, content: string) => Promise<void>;
  removeMessage: (messageId: string) => Promise<void>;
};

// loads history for whichever conversation is active - channel and DM
// history are the same concern (fetch, map, store) just against a
// different endpoint, so one effect covers both instead of two near-
// identical ones. Upward pagination is channel-only (matches the backlog).
export function useConversationHistory(
  mode: ConversationMode,
  conversationId: string | null,
  currentUserId: string | null
): ConversationHistory {
  const [messagesByConversation, setMessagesByConversation] = useState<
    Record<string, ChatMessageData[]>
  >({});
  const isFetchingOlderHistory = useRef<Record<string, boolean>>({});
  const hasMoreChannelHistory = useRef<Record<string, boolean>>({});

  useEffect(() => {
    if (!conversationId) {
      return;
    }

    let cancelled = false;

    async function loadChannelHistory(channelId: string) {
      const dtos = await listChannelMessages(channelId, { limit: MESSAGE_HISTORY_PAGE_SIZE });
      if (cancelled) {
        return;
      }

      hasMoreChannelHistory.current[channelId] = dtos.length >= MESSAGE_HISTORY_PAGE_SIZE;
      const mapped = [...dtos].reverse().map((dto) => mapChannelMessage(dto, currentUserId));
      setMessagesByConversation((current) => ({ ...current, [channelId]: mapped }));
    }

    async function loadDmHistory(partnerId: string) {
      try {
        const dtos = await listDirectMessageHistory(partnerId, { limit: MESSAGE_HISTORY_PAGE_SIZE });
        if (cancelled) {
          return;
        }

        const mapped = [...dtos].reverse().map((dto) => mapDirectMessage(dto, currentUserId));
        setMessagesByConversation((current) => ({ ...current, [partnerId]: mapped }));
      } catch (error) {
        if (cancelled) {
          return;
        }

        // no conversation started yet with this partner - an empty history, not an error
        if (error instanceof ApiError && error.status === 404) {
          setMessagesByConversation((current) => ({ ...current, [partnerId]: [] }));
        }
      }
    }

    if (mode === 'guild') {
      loadChannelHistory(conversationId).catch(() => {
        // best effort: leave any previously-loaded history in place
      });
    } else {
      loadDmHistory(conversationId);
    }

    return () => {
      cancelled = true;
    };
  }, [mode, conversationId, currentUserId]);

  async function loadOlderChannelHistory(channelId: string) {
    if (isFetchingOlderHistory.current[channelId] || hasMoreChannelHistory.current[channelId] === false) {
      return;
    }

    const oldestMessage = messagesByConversation[channelId]?.[0];
    if (!oldestMessage?.createdAt) {
      return;
    }

    isFetchingOlderHistory.current[channelId] = true;

    try {
      const dtos = await listChannelMessages(channelId, {
        before_time: oldestMessage.createdAt,
        limit: MESSAGE_HISTORY_PAGE_SIZE
      });

      hasMoreChannelHistory.current[channelId] = dtos.length >= MESSAGE_HISTORY_PAGE_SIZE;

      if (dtos.length > 0) {
        const olderMessages = [...dtos].reverse().map((dto) => mapChannelMessage(dto, currentUserId));
        setMessagesByConversation((current) => ({
          ...current,
          [channelId]: [...olderMessages, ...(current[channelId] ?? [])]
        }));
      }
    } catch {
      // best effort: leave existing history in place, retry on the next scroll-to-top
    } finally {
      isFetchingOlderHistory.current[channelId] = false;
    }
  }

  async function sendMessage(content: string) {
    if (!conversationId) {
      return;
    }

    const nonce = crypto.randomUUID();
    const nowIso = new Date().toISOString();
    const optimisticMessage: ChatMessageData = {
      id: nonce,
      authorId: currentUserId ?? undefined,
      author: authorLabel(currentUserId ?? '', currentUserId),
      accent: accentForAuthor(currentUserId ?? ''),
      content: splitMessageLines(content),
      timestamp: formatMessageTimestamp(nowIso),
      createdAt: nowIso,
      replyToId: null,
      attachments: []
    };

    setMessagesByConversation((current) => ({
      ...current,
      [conversationId]: [...(current[conversationId] ?? []), optimisticMessage]
    }));

    try {
      const mapped =
        mode === 'guild'
          ? mapChannelMessage(await sendChannelMessage(conversationId, { content, nonce }), currentUserId)
          : mapDirectMessage(await sendDirectMessage(conversationId, { content, nonce }), currentUserId);

      setMessagesByConversation((current) => ({
        ...current,
        [conversationId]: (current[conversationId] ?? []).map((message) =>
          message.id === nonce ? mapped : message
        )
      }));
    } catch {
      // 10-minute nonce dedup window means a retry can still land later - just flag this one as failed
      setMessagesByConversation((current) => ({
        ...current,
        [conversationId]: (current[conversationId] ?? []).map((message) =>
          message.id === nonce ? { ...message, failed: true } : message
        )
      }));
    }
  }

  async function updateMessage(messageId: string, content: string) {
    if (!conversationId) {
      return;
    }

    const response = await editMessageApi(messageId, { content });

    setMessagesByConversation((current) => ({
      ...current,
      [conversationId]: (current[conversationId] ?? []).map((message) =>
        message.id === messageId
          ? { ...message, content: splitMessageLines(response.content), editedAt: response.edited_at }
          : message
      )
    }));
  }

  async function removeMessage(messageId: string) {
    if (!conversationId) {
      return;
    }

    await deleteMessageApi(messageId);

    setMessagesByConversation((current) => ({
      ...current,
      [conversationId]: (current[conversationId] ?? []).filter((message) => message.id !== messageId)
    }));
  }

  return {
    messagesByConversation,
    setMessagesByConversation,
    loadOlderChannelHistory,
    sendMessage,
    updateMessage,
    removeMessage
  };
}
