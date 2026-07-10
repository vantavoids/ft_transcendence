import { apiFetch } from './client';

export type UserStatus = 'online' | 'idle' | 'dnd' | 'offline';

export type PublicUserProfileDto = {
  id: string;
  username: string;
  display_name: string | null;
  avatar_url: string | null;
  banner_url: string | null;
  status: UserStatus;
  bio: string | null;
  last_seen_at: string | null;
};

export type UserSummaryDto = {
  id: string;
  username: string;
  display_name: string | null;
  avatar_url: string | null;
  status: UserStatus;
};

export type UpdateUserProfilePayload = {
  display_name?: string;
  bio?: string;
  status?: UserStatus;
};

export type RelationshipStatus =
  | 'accepted'
  | 'pending_outgoing'
  | 'pending_incoming'
  | 'blocked_by_me'
  | 'blocked_by_them'
  | 'none';

export type RelationshipDto = {
  status: RelationshipStatus;
  since: string | null;
};

export type FriendSummaryDto = UserSummaryDto & {
  friendship_status: 'accepted';
};

export type FriendRequestDirection = 'incoming' | 'outgoing';

export type FriendRequestListItemDto = {
  friendship_id: string;
  direction: FriendRequestDirection;
  user: UserSummaryDto;
  created_at: string;
};

export type FriendshipDto = {
  id: string;
  requester_id: string;
  addressee_id: string;
  status: 'pending' | 'accepted' | 'blocked';
  created_at: string;
};

export type BlockListItemDto = {
  id: string;
  username: string;
  blocked_at: string;
};

export type UserProfileDto = PublicUserProfileDto;
export type FriendDto = {
  id: string;
  username: string;
  display_name: string | null;
  avatar_url: string | null;
  status: UserStatus;
  friendship_status: 'accepted' | 'pending' | 'blocked';
};

export type CreateFriendRequestPayload = {
  addressee_id: string;
};

export type UpdateFriendRequestPayload = {
  status: 'accepted' | 'blocked';
};

export function getCurrentUser() {
  return apiFetch<PublicUserProfileDto>('user', '/users/me');
}

export function getUser(userId: string) {
  return apiFetch<PublicUserProfileDto>('user', `/users/${userId}`);
}

export function listUsers(userIds: string[]) {
  return apiFetch<UserSummaryDto[]>('user', '/users', {
    query: { ids: userIds.join(',') }
  });
}

export function searchUsers(q: string, limit = 20) {
  return apiFetch<UserSummaryDto[]>('user', '/users/search', {
    query: { q, limit }
  });
}

export function updateUserProfile(userId: string, payload: UpdateUserProfilePayload) {
  return apiFetch<PublicUserProfileDto>('user', `/users/${userId}`, {
    method: 'PATCH',
    body: payload
  });
}

export function uploadUserAvatar(userId: string, avatar: File) {
  const body = new FormData();
  body.append('avatar', avatar);

  return apiFetch<{ avatar_url: string | null }>('user', `/users/${userId}/avatar`, {
    method: 'POST',
    body
  });
}

export function deleteUserAvatar(userId: string) {
  return apiFetch<void>('user', `/users/${userId}/avatar`, {
    method: 'DELETE'
  });
}

export function uploadUserBanner(userId: string, banner: File) {
  const body = new FormData();
  body.append('banner', banner);

  return apiFetch<{ banner_url: string | null }>('user', `/users/${userId}/banner`, {
    method: 'POST',
    body
  });
}

export function deleteUserBanner(userId: string) {
  return apiFetch<void>('user', `/users/${userId}/banner`, {
    method: 'DELETE'
  });
}

export function listFriends(userId: string) {
  return apiFetch<FriendSummaryDto[]>('user', `/users/${userId}/friends`);
}

export function listFriendRequests(direction?: FriendRequestDirection) {
  return apiFetch<FriendRequestListItemDto[]>('user', '/users/me/friend-requests', {
    query: direction ? { direction } : undefined
  });
}

export function getRelationship(userId: string) {
  return apiFetch<RelationshipDto>('user', `/users/me/friendship/${userId}`);
}

export function createFriendRequest(payload: CreateFriendRequestPayload) {
  return apiFetch<FriendshipDto>('user', '/friends', {
    method: 'POST',
    body: payload
  });
}

export function updateFriendRequest(friendshipId: string, payload: UpdateFriendRequestPayload) {
  return apiFetch<FriendshipDto>('user', `/friends/${friendshipId}`, {
    method: 'PATCH',
    body: payload
  });
}

export function deleteFriendRequest(friendshipId: string) {
  return apiFetch<void>('user', `/friends/${friendshipId}`, {
    method: 'DELETE'
  });
}

export function listBlockedUsers() {
  return apiFetch<BlockListItemDto[]>('user', '/users/me/blocks');
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
