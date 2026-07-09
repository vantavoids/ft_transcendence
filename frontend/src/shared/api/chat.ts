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

export type SendMessagePayload = {
  content?: string;
  reply_to_id?: string | null;
  attachment_ids?: string[];
  nonce?: string;
};

export type SendChannelMessageResponse = ChannelMessageDto & { nonce: string | null };

// unlike ChannelMessageDto's response, DM send echoes conversation_id and nonce
// but never edited_at/reactions (docs/contracts/chat.md:215-259)
export type SendDirectMessageResponse = {
  id: string;
  conversation_id: string;
  sender_id: string;
  recipient_id: string;
  content: string | null;
  reply_to_id: string | null;
  created_at: string;
  attachments: MessageAttachmentDto[];
  nonce: string | null;
};

export function listChannelMessages(
  channelId: string,
  query?: { before_time?: string; limit?: number }
) {
  return apiFetch<ChannelMessageDto[]>('chat', `/channels/${channelId}/messages`, { query });
}

export function sendChannelMessage(channelId: string, payload: SendMessagePayload) {
  return apiFetch<SendChannelMessageResponse>('chat', `/channels/${channelId}/messages`, {
    method: 'POST',
    body: payload
  });
}

export function sendDirectMessage(userId: string, payload: SendMessagePayload) {
  return apiFetch<SendDirectMessageResponse>('chat', `/dms/${userId}/messages`, {
    method: 'POST',
    body: payload
  });
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

export function uploadAttachment(file: File) {
  const formData = new FormData();
  formData.append('file', file);

  return apiFetch<MessageAttachmentDto>('chat', '/attachments', {
    method: 'POST',
    body: formData
  });
}

export type ReactionResponse = {
  emoji: string;
  count: number;
  me_reacted: boolean;
};

export function addReaction(messageId: string, emoji: string) {
  return apiFetch<ReactionResponse>(
    'chat',
    `/messages/${messageId}/reactions/${encodeURIComponent(emoji)}`,
    { method: 'PUT' }
  );
}

export function removeReaction(messageId: string, emoji: string) {
  return apiFetch<ReactionResponse>(
    'chat',
    `/messages/${messageId}/reactions/${encodeURIComponent(emoji)}`,
    { method: 'DELETE' }
  );
}

export type ChannelReadStateDto = {
  channel_id: string;
  last_read_message_id: string | null;
  unread_count: number;
};

export type DmReadStateResponse = {
  partner_id: string;
  last_read_message_id: string;
  unread_count: number;
};

export function markChannelRead(channelId: string, messageId: string) {
  return apiFetch<ChannelReadStateDto>('chat', `/channels/${channelId}/read`, {
    method: 'PUT',
    body: { message_id: messageId }
  });
}

export function markDirectMessageRead(userId: string, messageId: string) {
  return apiFetch<DmReadStateResponse>('chat', `/dms/${userId}/read`, {
    method: 'PUT',
    body: { message_id: messageId }
  });
}

export function listChannelReadStates() {
  return apiFetch<ChannelReadStateDto[]>('chat', '/channels/read-states');
}
