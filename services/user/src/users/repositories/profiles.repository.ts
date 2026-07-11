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

function isUniqueViolation(error: unknown): boolean {
  return (
    typeof error === 'object' &&
    error !== null &&
    'code' in error &&
    (error as { code?: string }).code === '23505'
  );
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

  async createProfileFromRegistration(
    userId: string,
    email: string,
  ): Promise<'created' | 'exists' | 'conflict'> {
    const usernames = this.buildCandidateUsernames(email, userId);

    for (const username of usernames) {
      try {
        const result = await this.database.client.query<{ id: string }>(
          `
            INSERT INTO users_profile (id, username, display_name, status)
            VALUES ($1::bigint, $2, $3, 'offline')
            ON CONFLICT (id) DO NOTHING
            RETURNING id::text
          `,
          [userId, username, this.buildDefaultDisplayName(username)],
        );

        if ((result.rowCount ?? 0) > 0) {
          return 'created';
        }

        const existing = await this.database.client.query<{ id: string }>(
          `
            SELECT id::text
            FROM users_profile
            WHERE id = $1::bigint
            LIMIT 1
          `,
          [userId],
        );

        if ((existing.rowCount ?? 0) > 0) {
          return 'exists';
        }
      } catch (error) {
        if (isUniqueViolation(error)) {
          continue;
        }

        throw error;
      }
    }

    return 'conflict';
  }

  async markUserOnline(userId: string): Promise<boolean> {
    const result = await this.database.client.query<{ id: string }>(
      `
        UPDATE users_profile
        SET
          status = 'online',
          updated_at = NOW()
        WHERE id = $1::bigint
        RETURNING id::text
      `,
      [userId],
    );

    return (result.rowCount ?? 0) > 0;
  }

  async markUserOffline(userId: string): Promise<boolean> {
    const result = await this.database.client.query<{ id: string }>(
      `
        UPDATE users_profile
        SET
          status = 'offline',
          last_seen_at = CASE
            WHEN status IS DISTINCT FROM 'offline' THEN NOW()
            ELSE last_seen_at
          END,
          updated_at = NOW()
        WHERE id = $1::bigint
        RETURNING id::text
      `,
      [userId],
    );

    return (result.rowCount ?? 0) > 0;
  }

  async deleteProfileById(userId: string): Promise<boolean> {
    const result = await this.database.client.query<{ id: string }>(
      `
        DELETE FROM users_profile
        WHERE id = $1::bigint
        RETURNING id::text
      `,
      [userId],
    );

    return (result.rowCount ?? 0) > 0;
  }

  private buildCandidateUsernames(email: string, userId: string): string[] {
    const atIndex = email.indexOf('@');
    const prefix = (atIndex >= 0 ? email.slice(0, atIndex) : email).trim();
    const base = prefix.slice(0, 32);
    const fallback =
      base.length <= 13 ? `${base}_${userId}` : `${base.slice(0, 13)}_${userId}`;

    return base === fallback ? [base] : [base, fallback];
  }

  private buildDefaultDisplayName(username: string): string {
    const humanized = username
      .replace(/[._-]+/g, ' ')
      .trim()
      .split(/\s+/)
      .filter(Boolean)
      .map((part) => part.charAt(0).toUpperCase() + part.slice(1).toLowerCase())
      .join(' ');

    return humanized || 'User';
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
