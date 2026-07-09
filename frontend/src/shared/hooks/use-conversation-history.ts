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
import { onChatHubEvent } from '../api/chat-hub';
import {
  accentForAuthor,
  authorLabel,
  formatMessageTimestamp,
  mapChannelMessage,
  mapDirectMessage,
  splitMessageLines
} from '../mappers/chat';

// nonce-aware upsert: a locally-sent message can arrive here before or after
// its own REST response already reconciled the optimistic bubble - match by
//  nonce first, then by id, else append.
function reconcileIncomingMessage(
  existing: ChatMessageData[],
  mapped: ChatMessageData,
  nonce: string | null
): ChatMessageData[] {
  if (nonce) {
    const nonceIndex = existing.findIndex((message) => message.id === nonce);
    if (nonceIndex >= 0) {
      return existing.map((message, index) => (index === nonceIndex ? mapped : message));
    }
  }

  if (existing.some((message) => message.id === mapped.id)) {
    return existing;
  }

  return [...existing, mapped];
}

function reconcileEditedMessage(
  current: Record<string, ChatMessageData[]>,
  containerId: string,
  messageId: string,
  content: string,
  editedAt: string
): Record<string, ChatMessageData[]> {
  const existing = current[containerId];
  if (!existing) {
    return current;
  }

  return {
    ...current,
    [containerId]: existing.map((message) =>
      message.id === messageId ? { ...message, content: splitMessageLines(content), editedAt } : message
    )
  };
}

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
  sendMessage: (content: string, replyToId?: string | null) => Promise<void>;
  retryMessage: (messageId: string) => Promise<void>;
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
  // attachment ids aren't kept on the (optimistic) ChatMessageData itself, so a
  // retry needs its own record of what was actually attached to a given send
  const pendingSendAttachmentIds = useRef<Record<string, string[]>>({});

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

  // real-time reconciliation - registered once (not per-conversation): the
  // hub delivers channel events to joined channels and DM events to the
  // caller's personal group regardless of which conversation is on screen,
  // so every event carries its own container id to route the state patch.
  useEffect(() => {
    const unsubscribers = [
      onChatHubEvent('ReceiveMessage', (event) => {
        const mapped = mapChannelMessage(event, currentUserId);
        setMessagesByConversation((current) => ({
          ...current,
          [event.channel_id]: reconcileIncomingMessage(
            current[event.channel_id] ?? [],
            mapped,
            event.nonce
          )
        }));
      }),
      onChatHubEvent('ReceiveDirectMessage', (event) => {
        const mapped = mapDirectMessage(event, currentUserId);
        const conversationKey = event.sender_id === currentUserId ? event.recipient_id : event.sender_id;
        setMessagesByConversation((current) => ({
          ...current,
          [conversationKey]: reconcileIncomingMessage(current[conversationKey] ?? [], mapped, event.nonce)
        }));
      }),
      onChatHubEvent('MessageEdited', (event) => {
        setMessagesByConversation((current) =>
          reconcileEditedMessage(current, event.channel_id, event.id, event.content, event.edited_at)
        );
      }),
      onChatHubEvent('MessageDeleted', (event) => {
        setMessagesByConversation((current) => {
          const existing = current[event.channel_id];
          if (!existing) {
            return current;
          }

          return {
            ...current,
            [event.channel_id]: existing.filter((message) => message.id !== event.message_id)
          };
        });
      }),
      // DirectMessageEdited/Deleted only carry conversation_id, but
      // messagesByConversation is keyed by partner user id - scan the small
      // set of open DM buckets for the message instead (message ids are
      // globally unique snowflakes).
      onChatHubEvent('DirectMessageEdited', (event) => {
        setMessagesByConversation((current) => {
          const containerId = Object.keys(current).find((key) =>
            current[key].some((message) => message.id === event.id)
          );
          if (!containerId) {
            return current;
          }

          return reconcileEditedMessage(current, containerId, event.id, event.content, event.edited_at);
        });
      }),
      onChatHubEvent('DirectMessageDeleted', (event) => {
        setMessagesByConversation((current) => {
          const key = Object.keys(current).find((k) =>
            current[k].some((message) => message.id === event.message_id)
          );
          if (!key) {
            return current;
          }

          return {
            ...current,
            [key]: current[key].filter((message) => message.id !== event.message_id)
          };
        });
      }),
      onChatHubEvent('ReactionAdded', (event) => {
        applyReactionEvent(event, true);
      }),
      onChatHubEvent('ReactionRemoved', (event) => {
        applyReactionEvent(event, false);
      })
    ];

    function applyReactionEvent(
      event: { message_id: string; channel_id: string; user_id: string; emoji: string; count: number },
      added: boolean
    ) {
      setMessagesByConversation((current) => {
        const existing = current[event.channel_id];
        if (!existing) {
          return current;
        }

        return {
          ...current,
          [event.channel_id]: existing.map((message) => {
            if (message.id !== event.message_id) {
              return message;
            }

            const otherReactions = (message.reactions ?? []).filter((r) => r.emoji !== event.emoji);
            const meReacted = event.user_id === currentUserId ? added : (message.reactions ?? []).some(
              (r) => r.emoji === event.emoji && r.meReacted
            );
            const nextReactions =
              event.count > 0 ? [...otherReactions, { emoji: event.emoji, count: event.count, meReacted }] : otherReactions;

            return { ...message, reactions: nextReactions.length > 0 ? nextReactions : undefined };
          })
        };
      });
    }

    return () => {
      unsubscribers.forEach((unsubscribe) => unsubscribe());
    };
  }, [currentUserId]);

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

  // delivers one send attempt to the right endpoint for the mode and maps
  // the response - shared by the initial send and a later retry of the same
  // nonce, both of which only differ in how the payload/optimistic bubble
  // were originally built.
  async function deliverMessage(
    targetConversationId: string,
    payload: SendMessagePayload
  ): Promise<ChatMessageData> {
    return mode === 'guild'
      ? mapChannelMessage(await sendChannelMessage(targetConversationId, payload), currentUserId)
      : mapDirectMessage(await sendDirectMessage(targetConversationId, payload), currentUserId);
  }

  async function sendMessage(content: string, replyToId: string | null = null) {
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
      replyToId,
      // real attachment metadata (url, mime_type, size) only exists once the
      // real response lands - avoid rendering a broken preview in the meantime
      attachments: []
    };

    setMessagesByConversation((current) => ({
      ...current,
      [conversationId]: [...(current[conversationId] ?? []), optimisticMessage]
    }));
    pendingSendAttachmentIds.current[nonce] = attachmentIds;

    const payload: SendMessagePayload = { nonce };
    if (content) {
      payload.content = content;
    }
    if (attachmentIds.length > 0) {
      payload.attachment_ids = attachmentIds;
    }
    if (replyToId) {
      payload.reply_to_id = replyToId;
    }

    try {
      const mapped = await deliverMessage(conversationId, payload);

      setMessagesByConversation((current) => ({
        ...current,
        [conversationId]: (current[conversationId] ?? []).map((message) =>
          message.id === nonce ? mapped : message
        )
      }));

      delete pendingSendAttachmentIds.current[nonce];
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

  // re-attempts a failed send with the same nonce - not a new one - so a
  // request that actually reached the server the first time (but whose
  // response was lost client-side) replays into the original message
  // instead of risking a second, real duplicate.
  async function retryMessage(messageId: string) {
    if (!conversationId) {
      return;
    }

    const target = (messagesByConversation[conversationId] ?? []).find(
      (message) => message.id === messageId
    );
    if (!target?.failed) {
      return;
    }

    setMessagesByConversation((current) => ({
      ...current,
      [conversationId]: (current[conversationId] ?? []).map((message) =>
        message.id === messageId ? { ...message, failed: false } : message
      )
    }));

    const attachmentIds = pendingSendAttachmentIds.current[messageId] ?? [];
    const payload: SendMessagePayload = { nonce: messageId };
    if (target.content.length > 0) {
      payload.content = target.content.join('\n');
    }
    if (attachmentIds.length > 0) {
      payload.attachment_ids = attachmentIds;
    }
    if (target.replyToId) {
      payload.reply_to_id = target.replyToId;
    }

    try {
      const mapped = await deliverMessage(conversationId, payload);

      setMessagesByConversation((current) => ({
        ...current,
        [conversationId]: (current[conversationId] ?? []).map((message) =>
          message.id === messageId ? mapped : message
        )
      }));
      delete pendingSendAttachmentIds.current[messageId];
    } catch {
      setMessagesByConversation((current) => ({
        ...current,
        [conversationId]: (current[conversationId] ?? []).map((message) =>
          message.id === messageId ? { ...message, failed: true } : message
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

    setMessagesByConversation((current) =>
      reconcileEditedMessage(current, conversationId, messageId, response.content, response.edited_at)
    );
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
    retryMessage,
    updateMessage,
    removeMessage,
    toggleReaction,
    pendingAttachments: conversationId ? (pendingAttachmentsByConversation[conversationId] ?? []) : [],
    uploadAttachments,
    removePendingAttachment
  };
}
