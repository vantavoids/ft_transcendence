import type { DirectMessage } from '../../components/dm-list';
import type { UserProfileDto, UserStatus } from '../api/user';

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

  return date.toLocaleString('en-US', {
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

