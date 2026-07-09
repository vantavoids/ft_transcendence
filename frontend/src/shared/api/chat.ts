import { apiFetch } from './client';

export type MessageAttachmentDto = {
  id: string;
  url: string;
  filename: string;
  size_bytes: number;
  mime_type: string;
};

export type MessageReactionDto = {
  emoji: string;
  count: number;
  me_reacted: boolean;
};

export type ChannelMessageDto = {
  id: string;
  channel_id: string;
  author_id: string;
  // NULL for attachment-only messages (docs/schema/chat.cql: messages.content)
  content: string | null;
  reply_to_id: string | null;
  edited_at: string | null;
  created_at: string;
  attachments: MessageAttachmentDto[];
  reactions: MessageReactionDto[];
};

export type DirectMessageConversationDto = {
  partner_id: string;
  last_preview: string;
  last_message_at: string;
  unread_count: number;
  is_archived: boolean;
};

export type DirectMessageDto = {
  id: string;
  sender_id: string;
  recipient_id: string;
  // NULL for attachment-only messages (docs/schema/chat.cql: direct_messages.content)
  content: string | null;
  reply_to_id: string | null;
  created_at: string;
  attachments: MessageAttachmentDto[];
};

export type EditMessagePayload = {
  content: string;
};

export type EditMessageResponse = {
  id: string;
  content: string;
  edited_at: string;
};

export function listChannelMessages(
  channelId: string,
  query?: { before_time?: string; limit?: number }
) {
  return apiFetch<ChannelMessageDto[]>('chat', `/channels/${channelId}/messages`, { query });
}

export function editMessage(messageId: string, payload: EditMessagePayload) {
  return apiFetch<EditMessageResponse>('chat', `/messages/${messageId}`, {
    method: 'PATCH',
    body: payload
  });
}

export function deleteMessage(messageId: string) {
  return apiFetch<void>('chat', `/messages/${messageId}`, {
    method: 'DELETE'
  });
}

export function listDirectMessages(query?: { include_archived?: boolean }) {
  return apiFetch<DirectMessageConversationDto[]>('chat', '/dms', { query });
}

export function listDirectMessageHistory(
  userId: string,
  query?: { before_time?: string; limit?: number }
) {
  return apiFetch<DirectMessageDto[]>('chat', `/dms/${userId}/messages`, { query });
}

export function archiveDirectMessageConversation(userId: string) {
  return apiFetch<void>('chat', `/dms/${userId}`, { method: 'DELETE' });
}
