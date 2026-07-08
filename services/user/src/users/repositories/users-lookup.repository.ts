import { Injectable } from '@nestjs/common';
import { DatabaseService } from '../../database/database.service';
import { UserProfileResponse, UserSummaryResponse } from '../users.types';

interface UserSummaryRow {
  id: string;
  username: string;
  display_name: string | null;
  avatar_url: string | null;
  status: UserProfileResponse['status'];
}

interface RequestedUserRow {
  id: string;
  ordinality: number;
}

@Injectable()
export class UsersLookupRepository {
  constructor(private readonly database: DatabaseService) {}

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
