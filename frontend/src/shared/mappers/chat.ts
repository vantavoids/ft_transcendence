import type { ChatMessageData } from '../../components/chat-message';
import type { ChannelMessageDto, MessageReactionDto } from '../api/chat';

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

// deterministic (not random) so the same author always renders with the same accent color
export function accentForAuthor(authorId: string): MessageAccent {
  let hash = 0;

  for (let index = 0; index < authorId.length; index += 1) {
    hash = (hash * 31 + authorId.charCodeAt(index)) | 0;
  }

  return MESSAGE_ACCENTS[Math.abs(hash) % MESSAGE_ACCENTS.length];
}

// no GET /users/{id} hydration yet (epic 2) - fall back to a raw-id placeholder
export function authorLabel(authorId: string, currentUserId: string | null): string {
  return authorId === currentUserId ? 'You' : `User ${authorId}`;
}

function reactionsToRecord(reactions: MessageReactionDto[]): Record<string, number> | undefined {
  if (reactions.length === 0) {
    return undefined;
  }

  return Object.fromEntries(reactions.map((reaction) => [reaction.emoji, reaction.count]));
}

export function mapChannelMessage(
  dto: ChannelMessageDto,
  currentUserId: string | null
): ChatMessageData {
  return {
    id: dto.id,
    authorId: dto.author_id,
    author: authorLabel(dto.author_id, currentUserId),
    accent: accentForAuthor(dto.author_id),
    content: splitMessageLines(dto.content ?? ''),
    timestamp: formatMessageTimestamp(dto.created_at),
    createdAt: dto.created_at,
    replyToId: dto.reply_to_id,
    editedAt: dto.edited_at,
    attachments: dto.attachments,
    reactions: reactionsToRecord(dto.reactions)
  };
}
