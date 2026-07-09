'use client';

import { useEffect, useRef, useState } from 'react';
import type { ChatMessageData } from '../../components/chat-message';
import {
  addReaction,
  deleteMessage as deleteMessageApi,
  editMessage as editMessageApi,
  listChannelMessages,
  listDirectMessageHistory,
  removeReaction,
  sendChannelMessage,
  sendDirectMessage,
  uploadAttachment,
  type SendMessagePayload
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

export type PendingAttachment = {
  id: string;
  filename: string;
  status: 'uploading' | 'ready' | 'error';
};

export type ConversationHistory = {
  messagesByConversation: Record<string, ChatMessageData[]>;
  setMessagesByConversation: React.Dispatch<
    React.SetStateAction<Record<string, ChatMessageData[]>>
  >;
  loadOlderChannelHistory: (channelId: string) => Promise<void>;
  sendMessage: (content: string) => Promise<void>;
  updateMessage: (messageId: string, content: string) => Promise<void>;
  removeMessage: (messageId: string) => Promise<void>;
  toggleReaction: (messageId: string, emoji: string) => Promise<void>;
  pendingAttachments: PendingAttachment[];
  uploadAttachments: (files: File[]) => void;
  removePendingAttachment: (attachmentId: string) => void;
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
  const [pendingAttachmentsByConversation, setPendingAttachmentsByConversation] = useState<
    Record<string, PendingAttachment[]>
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

    const readyAttachments = (pendingAttachmentsByConversation[conversationId] ?? []).filter(
      (attachment) => attachment.status === 'ready'
    );
    const attachmentIds = readyAttachments.map((attachment) => attachment.id);

    if (!content && attachmentIds.length === 0) {
      return;
    }

    const nonce = crypto.randomUUID();
    const nowIso = new Date().toISOString();
    const optimisticMessage: ChatMessageData = {
      id: nonce,
      authorId: currentUserId ?? undefined,
      author: authorLabel(currentUserId ?? '', currentUserId),
      accent: accentForAuthor(currentUserId ?? ''),
      content: content ? splitMessageLines(content) : [],
      timestamp: formatMessageTimestamp(nowIso),
      createdAt: nowIso,
      replyToId: null,
      // real attachment metadata (url, mime_type, size) only exists once the
      // real response lands - avoid rendering a broken preview in the meantime
      attachments: []
    };

    setMessagesByConversation((current) => ({
      ...current,
      [conversationId]: [...(current[conversationId] ?? []), optimisticMessage]
    }));

    const payload: SendMessagePayload = { nonce };
    if (content) {
      payload.content = content;
    }
    if (attachmentIds.length > 0) {
      payload.attachment_ids = attachmentIds;
    }

    try {
      const mapped =
        mode === 'guild'
          ? mapChannelMessage(await sendChannelMessage(conversationId, payload), currentUserId)
          : mapDirectMessage(await sendDirectMessage(conversationId, payload), currentUserId);

      setMessagesByConversation((current) => ({
        ...current,
        [conversationId]: (current[conversationId] ?? []).map((message) =>
          message.id === nonce ? mapped : message
        )
      }));

      if (attachmentIds.length > 0) {
        setPendingAttachmentsByConversation((current) => ({ ...current, [conversationId]: [] }));
      }
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

  function uploadAttachments(files: File[]) {
    if (!conversationId) {
      return;
    }

    const targetConversationId = conversationId;

    for (const file of files) {
      const draftId = `draft-${crypto.randomUUID()}`;

      setPendingAttachmentsByConversation((current) => ({
        ...current,
        [targetConversationId]: [
          ...(current[targetConversationId] ?? []),
          { id: draftId, filename: file.name, status: 'uploading' }
        ]
      }));

      uploadAttachment(file)
        .then((dto) => {
          setPendingAttachmentsByConversation((current) => ({
            ...current,
            [targetConversationId]: (current[targetConversationId] ?? []).map((attachment) =>
              attachment.id === draftId
                ? { id: dto.id, filename: dto.filename, status: 'ready' }
                : attachment
            )
          }));
        })
        .catch(() => {
          setPendingAttachmentsByConversation((current) => ({
            ...current,
            [targetConversationId]: (current[targetConversationId] ?? []).map((attachment) =>
              attachment.id === draftId ? { ...attachment, status: 'error' } : attachment
            )
          }));
        });
    }
  }

  function removePendingAttachment(attachmentId: string) {
    if (!conversationId) {
      return;
    }

    setPendingAttachmentsByConversation((current) => ({
      ...current,
      [conversationId]: (current[conversationId] ?? []).filter(
        (attachment) => attachment.id !== attachmentId
      )
    }));
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

  // channel messages only - the contract has no DM reaction table
  async function toggleReaction(messageId: string, emoji: string) {
    if (!conversationId || mode !== 'guild') {
      return;
    }

    const message = (messagesByConversation[conversationId] ?? []).find((m) => m.id === messageId);
    const alreadyReacted = message?.reactions?.some((r) => r.emoji === emoji && r.meReacted) ?? false;

    try {
      const response = alreadyReacted
        ? await removeReaction(messageId, emoji)
        : await addReaction(messageId, emoji);

      setMessagesByConversation((current) => ({
        ...current,
        [conversationId]: (current[conversationId] ?? []).map((m) => {
          if (m.id !== messageId) {
            return m;
          }

          const otherReactions = (m.reactions ?? []).filter((r) => r.emoji !== emoji);
          const nextReactions =
            response.count > 0
              ? [
                  ...otherReactions,
                  { emoji: response.emoji, count: response.count, meReacted: response.me_reacted }
                ]
              : otherReactions;

          return { ...m, reactions: nextReactions.length > 0 ? nextReactions : undefined };
        })
      }));
    } catch {
      // best effort: leave reactions as-is, allow the user to retry
    }
  }

  return {
    messagesByConversation,
    setMessagesByConversation,
    loadOlderChannelHistory,
    sendMessage,
    updateMessage,
    removeMessage,
    toggleReaction,
    pendingAttachments: conversationId ? (pendingAttachmentsByConversation[conversationId] ?? []) : [],
    uploadAttachments,
    removePendingAttachment
  };
}
