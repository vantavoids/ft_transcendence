import { apiFetch } from './client';

export type UserStatus = 'online' | 'idle' | 'dnd' | 'offline';

export type UserProfileDto = {
  id: string;
  username: string;
  display_name: string;
  avatar_url?: string | null;
  banner_url?: string | null;
  status: UserStatus;
  bio?: string | null;
  last_seen_at: string;
};

export type SearchUserDto = {
  id: string;
  username: string;
  display_name: string;
  avatar_url?: string | null;
  status: UserStatus;
};

export type UpdateUserProfilePayload = {
  display_name?: string;
  bio?: string;
  status?: UserStatus;
};

export type FriendDto = {
  id: string;
  friendship_id: string;
  username: string;
  display_name: string;
  avatar_url?: string | null;
  status: UserStatus;
  friendship_status: 'accepted' | 'pending' | 'blocked';
};

export type FriendshipDto = {
  id: string;
  requester_id: string;
  addressee_id: string;
  status: 'pending' | 'accepted' | 'blocked';
  created_at: string;
};

export type UserSummaryDto = {
  id: string;
  username: string;
  display_name: string;
  avatar_url?: string | null;
  banner_url?: string | null;
  status: UserStatus;
  bio?: string | null;
};

export type FriendRequestDto = {
  friendship_id: string;
  direction: 'incoming' | 'outgoing';
  user: UserSummaryDto;
  created_at: string;
};

export type FriendshipStateDto = {
  status:
    | 'accepted'
    | 'pending_outgoing'
    | 'pending_incoming'
    | 'blocked_by_me'
    | 'blocked_by_them'
    | 'none';
  since?: string | null;
};

export type BlockedUserDto = {
  id: string;
  username: string;
  blocked_at: string;
};

const USERS_BATCH_LIMIT = 100;

export async function getUsersByIds(ids: string[]): Promise<UserSummaryDto[]> {
  const uniqueIds = Array.from(new Set(ids));

  if (uniqueIds.length === 0) {
    return [];
  }

  const chunks: string[][] = [];
  for (let index = 0; index < uniqueIds.length; index += USERS_BATCH_LIMIT) {
    chunks.push(uniqueIds.slice(index, index + USERS_BATCH_LIMIT));
  }

  const results = await Promise.all(
    chunks.map((chunk) =>
      apiFetch<UserSummaryDto[]>('user', '/users', { query: { ids: chunk.join(',') } })
    )
  );

  return results.flat();
}

export function getCurrentUser() {
  return apiFetch<UserProfileDto>('user', '/users/me');
}

export function searchUsers(query: string, limit = 20) {
  return apiFetch<SearchUserDto[]>('user', '/users/search', {
    query: { q: query, limit }
  });
}

export function getUser(userId: string) {
  return apiFetch<UserProfileDto>('user', `/users/${userId}`);
}

export function updateUserProfile(userId: string, payload: UpdateUserProfilePayload) {
  return apiFetch<UserProfileDto>('user', `/users/${userId}`, {
    method: 'PATCH',
    body: payload
  });
}

export function listFriends(userId: string) {
  return apiFetch<FriendDto[]>('user', `/users/${userId}/friends`);
}

export function sendFriendRequest(addresseeId: string) {
  return apiFetch<FriendshipDto>('user', '/friends', {
    method: 'POST',
    body: { addressee_id: addresseeId }
  });
}

export function updateFriendship(friendshipId: string, status: 'accepted' | 'blocked') {
  return apiFetch<FriendshipDto>('user', `/friends/${friendshipId}`, {
    method: 'PATCH',
    body: { status }
  });
}

export function deleteFriendship(friendshipId: string) {
  return apiFetch<void>('user', `/friends/${friendshipId}`, {
    method: 'DELETE'
  });
}

export function listFriendRequests(direction: 'incoming' | 'outgoing' | 'all' = 'all') {
  return apiFetch<FriendRequestDto[]>('user', '/users/me/friend-requests', {
    query: { direction }
  });
}

export function getFriendshipState(userId: string) {
  return apiFetch<FriendshipStateDto>('user', `/users/me/friendship/${userId}`);
}

export function blockUser(userId: string) {
  return apiFetch<void>('user', `/users/${userId}/block`, {
    method: 'POST'
  });
}

export function unblockUser(userId: string) {
  return apiFetch<void>('user', `/users/${userId}/block`, {
    method: 'DELETE'
  });
}

export function listBlockedUsers() {
  return apiFetch<BlockedUserDto[]>('user', '/users/me/blocks');
}

export function uploadAvatar(userId: string, file: File) {
  const body = new FormData();
  body.set('avatar', file);

  return apiFetch<{ avatar_url: string }>('user', `/users/${userId}/avatar`, {
    method: 'POST',
    body
  });
}

export function removeAvatar(userId: string) {
  return apiFetch<void>('user', `/users/${userId}/avatar`, {
    method: 'DELETE'
  });
}

export function uploadBanner(userId: string, file: File) {
  const body = new FormData();
  body.set('banner', file);

  return apiFetch<{ banner_url: string }>('user', `/users/${userId}/banner`, {
    method: 'POST',
    body
  });
}

export function removeBanner(userId: string) {
  return apiFetch<void>('user', `/users/${userId}/banner`, {
    method: 'DELETE'
  });
}

export type DataExportStatusResponse = {
  export_id: string;
  status: 'pending' | 'ready' | 'failed';
  download_url?: string;
  expires_at?: string;
  error?: string;
};

export function requestDataExport() {
  return apiFetch<DataExportStatusResponse>('user', '/users/me/data-export', {
    method: 'POST'
  });
}

export function getDataExportStatus(exportId: string) {
  return apiFetch<DataExportStatusResponse>('user', `/users/me/data-export/${exportId}`);
}
