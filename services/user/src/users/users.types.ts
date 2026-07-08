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
  status: UserProfileResponse['status'];
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
