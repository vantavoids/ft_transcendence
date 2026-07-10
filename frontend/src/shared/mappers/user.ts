import type { ChatMessageData } from '../../components/chat-message';
import type { DirectMessage } from '../../components/dm-list';
import type { Friend } from '../../components/friends-list';
import type { UserProfileDto, UserStatus } from '../api/user';

const ACCENTS: ChatMessageData['accent'][] = ['aqua', 'yellow', 'lime', 'lavender', 'pink'];

export type CurrentUserProfile = {
  id: string;
  username: string;
  displayName: string;
  avatarUrl: string | null;
  bannerUrl: string | null;
  status: UserStatus;
  bio: string | null;
  lastSeenAt: string;
  lastSeenLabel: string;
};

function hashToAccent(id: string): ChatMessageData['accent'] {
  let hash = 0;
  for (let index = 0; index < id.length; index += 1) {
    hash = (hash * 31 + id.charCodeAt(index)) >>> 0;
  }
  return ACCENTS[hash % ACCENTS.length];
}

export function accentForUserId(id: string): ChatMessageData['accent'] {
  return hashToAccent(id);
}

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

function formatRelativeTime(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return '';
  }

  return date.toLocaleString('fr-FR', {
    day: '2-digit',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit'
  });
}

export function toCurrentUserProfile(user: UserProfileDto): CurrentUserProfile {
  return {
    id: user.id,
    username: user.username,
    displayName: user.display_name || user.username,
    avatarUrl: user.avatar_url ?? null,
    bannerUrl: user.banner_url ?? null,
    status: user.status,
    bio: user.bio ?? null,
    lastSeenAt: user.last_seen_at,
    lastSeenLabel: formatRelativeTime(user.last_seen_at)
  };
}

export function toUserStatusLabel(status: UserStatus): string {
  switch (status) {
    case 'online':
      return 'Online';
    case 'idle':
      return 'Idle';
    case 'dnd':
      return 'Do not disturb';
    default:
      return 'Offline';
  }
}

export function toFriend(friend: { id: string; username: string; display_name: string; status: UserStatus; friendship_status: 'accepted' | 'pending' | 'blocked'; }): Friend {
  return {
    id: friend.id,
    name: friend.display_name || friend.username,
    status: toSidebarStatus(friend.status),
    accent: accentForUserId(friend.id),
    note: friend.friendship_status === 'pending' ? 'Pending request' : ''
  };
}
