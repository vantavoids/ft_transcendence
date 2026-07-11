import type { ChatMessageData, ReactionSummary } from '../../components/chat-message';
import type { DirectMessage } from '../../components/dm-list';
import type {
  ChannelMessageDto,
  DirectMessageConversationDto,
  DirectMessageDto,
  MessageReactionDto
} from '../api/chat';
import type { UserSummaryDto } from '../api/user';
import { hashToIndex } from '../lib/hash';

type MessageAccent = ChatMessageData['accent'];

const MESSAGE_ACCENTS: MessageAccent[] = ['aqua', 'yellow', 'lime', 'lavender', 'pink'];

export function formatMessageTimestamp(isoTimestamp: string): string {
  return new Date(isoTimestamp).toLocaleTimeString('fr-FR', {
    hour: '2-digit',
    minute: '2-digit'
  });
}

export function splitMessageLines(content: string): string[] {
  return content.split(/\r?\n/);
}

export function accentForAuthor(authorId: string): MessageAccent {
  return MESSAGE_ACCENTS[hashToIndex(authorId, MESSAGE_ACCENTS.length)];
}

export function authorLabel(
  authorId: string,
  currentUserId: string | null,
  usersById: Record<string, UserSummaryDto> = {}
): string {
  if (authorId === currentUserId) {
    return 'You';
  }

  const profile = usersById[authorId];
  if (profile) {
    return profile.display_name || profile.username || `User ${authorId}`;
  }

  return `User ${authorId}`;
}

function mapReactions(reactions: MessageReactionDto[]): ReactionSummary[] | undefined {
  if (reactions.length === 0) {
    return undefined;
  }

  return reactions.map(({emoji, count, me_reacted}) => ({
    emoji,
    count,
    meReacted: me_reacted
  }));
}

export function mapChannelMessage(
  dto: ChannelMessageDto,
  currentUserId: string | null,
  usersById: Record<string, UserSummaryDto> = {}
): ChatMessageData {
  return {
    id: dto.id,
    authorId: dto.author_id,
    author: authorLabel(dto.author_id, currentUserId, usersById),
    accent: accentForAuthor(dto.author_id),
    content: splitMessageLines(dto.content ?? ''),
    timestamp: formatMessageTimestamp(dto.created_at),
    createdAt: dto.created_at,
    replyToId: dto.reply_to_id,
    editedAt: dto.edited_at,
    attachments: dto.attachments,
    reactions: mapReactions(dto.reactions)
  };
}

// DMs never carry edited_at or reactions in this response shape (docs/contracts/chat.md)
export function mapDirectMessage(
  dto: DirectMessageDto,
  currentUserId: string | null,
  usersById: Record<string, UserSummaryDto> = {}
): ChatMessageData {
  return {
    id: dto.id,
    authorId: dto.sender_id,
    author: authorLabel(dto.sender_id, currentUserId, usersById),
    accent: accentForAuthor(dto.sender_id),
    content: splitMessageLines(dto.content ?? ''),
    timestamp: formatMessageTimestamp(dto.created_at),
    createdAt: dto.created_at,
    replyToId: dto.reply_to_id,
    attachments: dto.attachments
  };
}

export function mapDirectMessageConversation(
  dto: DirectMessageConversationDto,
  usersById: Record<string, UserSummaryDto> = {}
): DirectMessage {
  const profile = usersById[dto.partner_id];
  return {
    id: dto.partner_id,
    name: profile?.display_name || profile?.username || `User ${dto.partner_id}`,
    status: 'offline',
    accent: accentForAuthor(dto.partner_id),
    lastMessage: dto.last_preview,
    lastMessageAt: formatMessageTimestamp(dto.last_message_at),
    lastActivityAt: Date.parse(dto.last_message_at),
    unreadCount: dto.unread_count,
    isArchived: dto.is_archived
  };
}
