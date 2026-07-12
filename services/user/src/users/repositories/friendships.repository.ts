import { Injectable } from '@nestjs/common';
import { DatabaseService } from '../../database/database.service';
import {
  FriendRequestDirection,
  FriendRequestListItemResponse,
  FriendRequestSentEvent,
  FriendSummaryResponse,
  FriendshipResponse,
  UserSummaryResponse,
} from '../users.types';

interface FriendshipRow {
  id: string;
  requester_id: string;
  addressee_id: string;
  status: 'pending' | 'accepted' | 'blocked';
  created_at: Date;
  updated_at: Date;
}

interface UserSummaryRow {
  id: string;
  username: string;
  display_name: string | null;
  avatar_url: string | null;
  banner_url: string | null;
  status: UserSummaryResponse['status'];
  bio: string | null;
}

interface FriendListRow extends UserSummaryRow {
  friendship_id: string;
}

interface FriendRequestRow extends UserSummaryRow {
  friendship_id: string;
  direction: Exclude<FriendRequestDirection, 'all'>;
  created_at: Date;
}

@Injectable()
export class FriendshipsRepository {
  constructor(private readonly database: DatabaseService) {}

  async listFriends(
    viewerId: string,
    userId: string,
  ): Promise<FriendSummaryResponse[]> {
    const result = await this.database.client.query<FriendListRow>(
      `
        SELECT
          friendship.id::text AS friendship_id,
          profile.id::text,
          profile.username,
          profile.display_name,
          profile.avatar_url,
          profile.status
        FROM friendships AS friendship
        JOIN users_profile AS profile
          ON profile.id = CASE
            WHEN friendship.requester_id = $2::bigint THEN friendship.addressee_id
            ELSE friendship.requester_id
          END
        WHERE friendship.status = 'accepted'
          AND (friendship.requester_id = $2::bigint OR friendship.addressee_id = $2::bigint)
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
      `,
      [viewerId, userId],
    );

    return result.rows.map((row) => this.toFriendSummary(row));
  }

  // accepted-friend ids for real-time fan-out (presence, profile, social).
  // excludes either-direction blocks so a blocked user never receives the
  // subject's presence, matching listFriends' visibility rules.
  async listFriendIds(userId: string): Promise<string[]> {
    const result = await this.database.client.query<{ id: string }>(
      `
        SELECT
          CASE
            WHEN friendship.requester_id = $1::bigint THEN friendship.addressee_id::text
            ELSE friendship.requester_id::text
          END AS id
        FROM friendships AS friendship
        WHERE friendship.status = 'accepted'
          AND (friendship.requester_id = $1::bigint OR friendship.addressee_id = $1::bigint)
          AND NOT EXISTS (
            SELECT 1
            FROM user_blocks AS block
            WHERE block.blocker_id = $1::bigint
              AND block.blocked_id = CASE
                WHEN friendship.requester_id = $1::bigint THEN friendship.addressee_id
                ELSE friendship.requester_id
              END
          )
          AND NOT EXISTS (
            SELECT 1
            FROM user_blocks AS block
            WHERE block.blocked_id = $1::bigint
              AND block.blocker_id = CASE
                WHEN friendship.requester_id = $1::bigint THEN friendship.addressee_id
                ELSE friendship.requester_id
              END
          )
      `,
      [userId],
    );

    return result.rows.map((row) => row.id);
  }

  async listFriendRequests(
    viewerId: string,
    direction: FriendRequestDirection,
  ): Promise<FriendRequestListItemResponse[]> {
    const result = await this.database.client.query<FriendRequestRow>(
      `
        SELECT
          friendship.id::text AS friendship_id,
          profile.id::text,
          profile.username,
          profile.display_name,
          profile.avatar_url,
          profile.banner_url,
          profile.status,
          profile.bio,
          CASE
            WHEN friendship.requester_id = $1::bigint THEN 'outgoing'
            ELSE 'incoming'
          END AS direction,
          friendship.created_at
        FROM friendships AS friendship
        JOIN users_profile AS profile
          ON profile.id = CASE
            WHEN friendship.requester_id = $1::bigint THEN friendship.addressee_id
            ELSE friendship.requester_id
          END
        WHERE friendship.status = 'pending'
          AND (friendship.requester_id = $1::bigint OR friendship.addressee_id = $1::bigint)
          AND (
            $2::text = 'all'
            OR ($2::text = 'incoming' AND friendship.addressee_id = $1::bigint)
            OR ($2::text = 'outgoing' AND friendship.requester_id = $1::bigint)
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
        ORDER BY friendship.created_at DESC, friendship.id DESC
      `,
      [viewerId, direction],
    );

    return result.rows.map((row) => this.toFriendRequest(row));
  }

  async createFriendRequest(
    friendshipId: string,
    requesterId: string,
    addresseeId: string,
  ): Promise<FriendshipResponse | 'not_found' | 'conflict'> {
    const addressee = await this.database.client.query<{ id: string }>(
      `
        SELECT id::text
        FROM users_profile
        WHERE id = $1::bigint
        LIMIT 1
      `,
      [addresseeId],
    );
    if (addressee.rowCount === 0) {
      return 'not_found';
    }

    const created = await this.database.client.query<FriendshipRow>(
      `
        WITH existing AS (
          SELECT 1
          FROM friendships
          WHERE LEAST(requester_id, addressee_id) = LEAST($2::bigint, $3::bigint)
            AND GREATEST(requester_id, addressee_id) = GREATEST($2::bigint, $3::bigint)
          LIMIT 1
        ),
        blocked AS (
          SELECT 1
          FROM user_blocks
          WHERE (blocker_id = $2::bigint AND blocked_id = $3::bigint)
             OR (blocker_id = $3::bigint AND blocked_id = $2::bigint)
          LIMIT 1
        )
        INSERT INTO friendships (id, requester_id, addressee_id, status)
        SELECT $1::bigint, $2::bigint, $3::bigint, 'pending'
        WHERE NOT EXISTS (SELECT 1 FROM existing)
          AND NOT EXISTS (SELECT 1 FROM blocked)
        RETURNING
          id::text,
          requester_id::text,
          addressee_id::text,
          status,
          created_at,
          updated_at
      `,
      [friendshipId, requesterId, addresseeId],
    );

    const row = created.rows[0];
    if (!row) {
      return 'conflict';
    }

    return this.toFriendshipResponse(row);
  }

  async updateFriendRequest(
    friendshipId: string,
    callerId: string,
    status: 'accepted' | 'blocked',
  ): Promise<FriendshipResponse | 'not_found' | 'forbidden' | 'conflict'> {
    const friendship = await this.database.client.query<FriendshipRow>(
      `
        SELECT
          id::text,
          requester_id::text,
          addressee_id::text,
          status,
          created_at,
          updated_at
        FROM friendships
        WHERE id = $1::bigint
        LIMIT 1
      `,
      [friendshipId],
    );

    const row = friendship.rows[0];
    if (!row) {
      return 'not_found';
    }

    if (row.addressee_id !== callerId) {
      return 'forbidden';
    }

    if (row.status !== 'pending') {
      return 'conflict';
    }

    const updated = await this.database.client.query<FriendshipRow>(
      `
        UPDATE friendships
        SET status = $2::friendship_status,
            updated_at = NOW()
        WHERE id = $1::bigint
          AND addressee_id = $3::bigint
          AND status = 'pending'
        RETURNING
          id::text,
          requester_id::text,
          addressee_id::text,
          status,
          created_at,
          updated_at
      `,
      [friendshipId, status, callerId],
    );

    const updatedRow = updated.rows[0];
    if (!updatedRow) {
      return 'conflict';
    }

    if (status === 'blocked') {
      await this.database.client.query(
        `
          INSERT INTO user_blocks (blocker_id, blocked_id)
          VALUES ($1::bigint, $2::bigint)
          ON CONFLICT DO NOTHING
        `,
        [callerId, row.requester_id],
      );
    }

    return this.toFriendshipResponse(updatedRow);
  }

  async deleteFriendRequest(
    friendshipId: string,
    callerId: string,
  ): Promise<'not_found' | 'forbidden' | 'deleted'> {
    const friendship = await this.database.client.query<FriendshipRow>(
      `
        SELECT
          id::text,
          requester_id::text,
          addressee_id::text,
          status,
          created_at,
          updated_at
        FROM friendships
        WHERE id = $1::bigint
        LIMIT 1
      `,
      [friendshipId],
    );

    const row = friendship.rows[0];
    if (!row) {
      return 'not_found';
    }

    if (row.requester_id !== callerId && row.addressee_id !== callerId) {
      return 'forbidden';
    }

    await this.database.client.query(
      `
        DELETE FROM friendships
        WHERE id = $1::bigint
      `,
      [friendshipId],
    );

    return 'deleted';
  }

  private toFriendSummary(row: FriendListRow): FriendSummaryResponse {
    return {
      id: row.id,
      username: row.username,
      display_name: row.display_name,
      avatar_url: row.avatar_url,
      status: row.status,
      friendship_status: 'accepted',
    };
  }

  private toFriendRequest(row: FriendRequestRow): FriendRequestListItemResponse {
    return {
      friendship_id: row.friendship_id,
      direction: row.direction,
      user: {
        id: row.id,
        username: row.username,
        display_name: row.display_name,
        avatar_url: row.avatar_url,
        banner_url: row.banner_url,
        status: row.status,
        bio: row.bio,
      },
      created_at: row.created_at.toISOString(),
    };
  }

  private toFriendshipResponse(row: FriendshipRow): FriendshipResponse {
    return {
      id: row.id,
      requester_id: row.requester_id,
      addressee_id: row.addressee_id,
      status: row.status,
      created_at: row.created_at.toISOString(),
    };
  }
}
