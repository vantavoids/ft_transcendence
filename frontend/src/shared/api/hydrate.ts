import type { ChatMessageData } from '../../components/chat-message';
import type { DirectMessage } from '../../components/dm-list';
import type { Friend } from '../../components/friends-list';
import type { DirectMessageConversationDto } from './chat';
import type { FriendDto, UserProfileDto, UserStatus } from './user';

const ACCENTS: ChatMessageData['accent'][] = ['aqua', 'yellow', 'lime', 'lavender', 'pink'];

// deterministic accent so a given user keeps the same colour across renders
// (the avatar chips are colour-coded but the API carries no accent field).
export function accentForId(id: string): ChatMessageData['accent'] {
  let hash = 0;
  for (let i = 0; i < id.length; i += 1) {
    hash = (hash * 31 + id.charCodeAt(i)) >>> 0;
  }
  return ACCENTS[hash % ACCENTS.length];
}

// the sidebar only renders three presence dots; collapse the richer user status
// set onto them (dnd is shown idle-style, anything unknown as offline).
export function toSidebarStatus(status: UserStatus): DirectMessage['status'] {
  switch (status) {
    case 'online':
      return 'online';
    case 'idle':
    case 'dnd':
      return 'idle';
    default:
      return 'offline';
  }
}

function toClockLabel(iso: string | null | undefined): string {
  if (!iso) {
    return '';
  }
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return '';
  }
  return date.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
}

export function toDirectMessage(
  conversation: DirectMessageConversationDto,
  partner: UserProfileDto
): DirectMessage {
  return {
    id: conversation.partner_id,
    name: partner.display_name || partner.username,
    status: toSidebarStatus(partner.status),
    accent: accentForId(conversation.partner_id),
    lastMessage: conversation.last_preview,
    lastMessageAt: toClockLabel(conversation.last_message_at),
    lastActivityAt: Date.parse(conversation.last_message_at),
    unreadCount: conversation.unread_count,
    isArchived: conversation.is_archived
  };
}

export function toFriend(friend: FriendDto): Friend {
  return {
    id: friend.id,
    friendshipId: friend.friendship_id,
    name: friend.display_name || friend.username,
    status: toSidebarStatus(friend.status),
    accent: accentForId(friend.id),
    note: friend.friendship_status === 'pending' ? 'Pending request' : ''
  };
}
