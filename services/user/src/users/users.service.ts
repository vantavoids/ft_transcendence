import { Injectable } from '@nestjs/common';
import { DatabaseService } from '../database/database.service';

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

export interface RelationshipResponse {
  status: RelationshipStatus;
  since: string | null;
}

interface UserProfileRow {
  id: string;
  username: string;
  display_name: string | null;
  avatar_url: string | null;
  banner_url: string | null;
  status: UserProfileResponse['status'];
  bio: string | null;
  last_seen_at: Date | null;
}

interface FriendshipRow {
  requester_id: string;
  addressee_id: string;
  status: 'pending' | 'accepted' | 'blocked';
  created_at: Date;
  updated_at: Date;
}

interface BlockRow {
  created_at: Date;
}

@Injectable()
export class UsersService {
  constructor(private readonly database: DatabaseService) {}

  async getInternalProfile(userId: string): Promise<UserProfileResponse | null> {
    const result = await this.database.client.query<UserProfileRow>(
      `
        SELECT
          id::text,
          username,
          display_name,
          avatar_url,
          banner_url,
          status,
          bio,
          last_seen_at
        FROM users_profile
        WHERE id = $1::bigint
        LIMIT 1
      `,
      [userId],
    );

    const row = result.rows[0];
    if (!row) {
      return null;
    }

    return this.toProfileResponse(row);
  }

  async getRelationshipPerspective(
    callerId: string,
    otherUserId: string,
  ): Promise<RelationshipResponse | null> {
    const usersExist = await this.database.client.query<{ id: string }>(
      `
        SELECT id::text
        FROM users_profile
        WHERE id = ANY(ARRAY[$1::bigint, $2::bigint])
      `,
      [callerId, otherUserId],
    );

    if (usersExist.rowCount !== 2) {
      return null;
    }

    const blockedByMe = await this.database.client.query<BlockRow>(
      `
        SELECT created_at
        FROM user_blocks
        WHERE blocker_id = $1::bigint
          AND blocked_id = $2::bigint
        LIMIT 1
      `,
      [callerId, otherUserId],
    );
    if (blockedByMe.rows.length > 0) {
      return {
        status: 'blocked_by_me',
        since: blockedByMe.rows[0].created_at.toISOString(),
      };
    }

    const blockedByThem = await this.database.client.query<BlockRow>(
      `
        SELECT created_at
        FROM user_blocks
        WHERE blocker_id = $1::bigint
          AND blocked_id = $2::bigint
        LIMIT 1
      `,
      [otherUserId, callerId],
    );
    if (blockedByThem.rows.length > 0) {
      return {
        status: 'blocked_by_them',
        since: null,
      };
    }

    const friendship = await this.database.client.query<FriendshipRow>(
      `
        SELECT
          requester_id::text,
          addressee_id::text,
          status,
          created_at,
          updated_at
        FROM friendships
        WHERE LEAST(requester_id, addressee_id) = LEAST($1::bigint, $2::bigint)
          AND GREATEST(requester_id, addressee_id) = GREATEST($1::bigint, $2::bigint)
        LIMIT 1
      `,
      [callerId, otherUserId],
    );

    const row = friendship.rows[0];
    if (!row) {
      return {
        status: 'none',
        since: null,
      };
    }

    if (row.status === 'accepted') {
      return {
        status: 'accepted',
        since: row.updated_at.toISOString(),
      };
    }

    if (row.requester_id === callerId) {
      return {
        status: 'pending_outgoing',
        since: row.created_at.toISOString(),
      };
    }

    return {
      status: 'pending_incoming',
      since: row.created_at.toISOString(),
    };
  }

  private toProfileResponse(row: UserProfileRow): UserProfileResponse {
    return {
      id: row.id,
      username: row.username,
      display_name: row.display_name,
      avatar_url: row.avatar_url,
      banner_url: row.banner_url,
      status: row.status,
      bio: row.bio,
      last_seen_at: row.last_seen_at ? row.last_seen_at.toISOString() : null,
    };
  }
}
