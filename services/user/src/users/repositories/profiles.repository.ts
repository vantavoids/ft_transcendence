import { Injectable } from '@nestjs/common';
import { DatabaseService } from '../../database/database.service';
import { UpdateUserProfileInput, UserProfileResponse } from '../users.types';

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

@Injectable()
export class ProfilesRepository {
  constructor(private readonly database: DatabaseService) {}

  async getProfileById(userId: string): Promise<UserProfileResponse | null> {
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

  async updateProfileById(
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
