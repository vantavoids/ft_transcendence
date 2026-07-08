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

interface UserSummaryRow {
  id: string;
  username: string;
  display_name: string | null;
  avatar_url: string | null;
  status: UserProfileResponse['status'];
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

interface RequestedUserRow {
  id: string;
  ordinality: number;
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

  async getUsersByIds(
    viewerId: string,
    userIds: string[],
  ): Promise<UserSummaryResponse[]> {
    if (userIds.length === 0) {
      return [];
    }

    const result = await this.database.client.query<
      UserSummaryRow & RequestedUserRow
    >(
      `
        WITH requested AS (
          SELECT *
          FROM unnest($2::bigint[]) WITH ORDINALITY AS ids(id, ordinality)
        )
        SELECT
          profile.id::text,
          profile.username,
          profile.display_name,
          profile.avatar_url,
          profile.status,
          requested.ordinality
        FROM requested
        JOIN users_profile AS profile
          ON profile.id = requested.id
        WHERE NOT EXISTS (
          SELECT 1
          FROM user_blocks AS block
          WHERE block.blocker_id = $1::bigint
            AND block.blocked_id = profile.id
        )
        AND NOT EXISTS (
          SELECT 1
          FROM user_blocks AS block
          WHERE block.blocker_id = profile.id
            AND block.blocked_id = $1::bigint
        )
        ORDER BY requested.ordinality ASC
      `,
      [viewerId, userIds],
    );

    return result.rows.map((row) => this.toSummaryResponse(row));
  }

  async searchUsers(
    viewerId: string,
    query: string,
    limit: number,
  ): Promise<UserSummaryResponse[]> {
    const escapedQuery = query.replace(/[\\%_]/g, '\\$&');
    const result = await this.database.client.query<UserSummaryRow>(
      `
        SELECT
          profile.id::text,
          profile.username,
          profile.display_name,
          profile.avatar_url,
          profile.status
        FROM users_profile AS profile
        WHERE (
          profile.username ILIKE $2 ESCAPE '\\'
          OR COALESCE(profile.display_name, '') ILIKE $2 ESCAPE '\\'
        )
        AND NOT EXISTS (
          SELECT 1
          FROM user_blocks AS block
          WHERE block.blocker_id = $1::bigint
            AND block.blocked_id = profile.id
        )
        AND NOT EXISTS (
          SELECT 1
          FROM user_blocks AS block
          WHERE block.blocker_id = profile.id
            AND block.blocked_id = $1::bigint
        )
        ORDER BY profile.username ASC, profile.id ASC
        LIMIT $3
      `,
      [viewerId, `%${escapedQuery}%`, limit],
    );

    return result.rows.map((row) => this.toSummaryResponse(row));
  }

  async updateInternalProfile(
    userId: string,
    changes: UpdateUserProfileInput,
  ): Promise<UserProfileResponse | null> {
    const result = await this.database.client.query<UserProfileRow>(
      `
        WITH current_profile AS (
          SELECT status
          FROM users_profile
          WHERE id = $1::bigint
          LIMIT 1
        )
        UPDATE users_profile AS profile
        SET
          display_name = COALESCE($2, profile.display_name),
          bio = COALESCE($3, profile.bio),
          status = COALESCE($4, profile.status),
          last_seen_at = CASE
            WHEN COALESCE($4, profile.status) = 'offline'
              AND (SELECT status FROM current_profile) IS DISTINCT FROM 'offline'
            THEN NOW()
            ELSE profile.last_seen_at
          END,
          updated_at = NOW()
        WHERE profile.id = $1::bigint
        RETURNING
          profile.id::text,
          profile.username,
          profile.display_name,
          profile.avatar_url,
          profile.banner_url,
          profile.status,
          profile.bio,
          profile.last_seen_at
      `,
      [
        userId,
        changes.display_name ?? null,
        changes.bio ?? null,
        changes.status ?? null,
      ],
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

  private toSummaryResponse(row: UserSummaryRow): UserSummaryResponse {
    return {
      id: row.id,
      username: row.username,
      display_name: row.display_name,
      avatar_url: row.avatar_url,
      status: row.status,
    };
  }
}
