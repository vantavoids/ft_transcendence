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

export type UpdateUserProfilePayload = {
  display_name?: string;
  bio?: string;
  status?: UserStatus;
};

export type FriendDto = {
  id: string;
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

export function getCurrentUser() {
  return apiFetch<UserProfileDto>('user', '/users/me');
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
