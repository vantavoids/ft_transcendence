export type RelationshipStatus =
  | 'accepted'
  | 'pending_outgoing'
  | 'pending_incoming'
  | 'blocked_by_me'
  | 'blocked_by_them'
  | 'none';

export interface UserProfileResponse {
  id: string;
  username: string;
  display_name: string | null;
  avatar_url: string | null;
  banner_url: string | null;
  status: 'online' | 'idle' | 'dnd' | 'offline';
  bio: string | null;
  last_seen_at: string | null;
}

export interface UserSummaryResponse {
  id: string;
  username: string;
  display_name: string | null;
  avatar_url: string | null;
  banner_url: string | null;
  status: UserProfileResponse['status'];
  bio: string | null;
}

export interface RelationshipResponse {
  status: RelationshipStatus;
  since: string | null;
}

export interface UpdateUserProfileInput {
  display_name?: string;
  bio?: string;
  status?: UserProfileResponse['status'];
}

export interface FriendSummaryResponse {
  id: string;
  username: string;
  display_name: string | null;
  avatar_url: string | null;
  status: UserProfileResponse['status'];
  friendship_status: 'accepted';
}

export type FriendRequestDirection = 'incoming' | 'outgoing' | 'all';

export interface FriendRequestListItemResponse {
  friendship_id: string;
  direction: Exclude<FriendRequestDirection, 'all'>;
  user: UserSummaryResponse;
  created_at: string;
}

export interface FriendshipResponse {
  id: string;
  requester_id: string;
  addressee_id: string;
  status: 'pending' | 'accepted' | 'blocked';
  created_at: string;
}

export interface BlockListItemResponse {
  id: string;
  username: string;
  blocked_at: string;
}

export interface FriendRequestSentEvent {
  friendship_id: string;
  requester_id: string;
  addressee_id: string;
}

export type UserDataExportFriendState =
  | 'accepted'
  | 'pending_outgoing'
  | 'pending_incoming';

export interface UserDataExportProfileResponse {
  username: string | null;
  display_name: string | null;
  avatar_url: string | null;
  banner_url: string | null;
  bio: string | null;
  status: UserProfileResponse['status'] | null;
  last_seen_at: string | null;
  created_at: string | null;
}

export interface UserDataExportFriendResponse {
  username: string;
  state: UserDataExportFriendState;
  since: string;
}

export interface UserDataExportBlockedUserResponse {
  username: string;
  blocked_at: string;
}

export interface UserDataExportResponse {
  user_id: string;
  profile: UserDataExportProfileResponse;
  friends: UserDataExportFriendResponse[];
  blocked_users: UserDataExportBlockedUserResponse[];
}
