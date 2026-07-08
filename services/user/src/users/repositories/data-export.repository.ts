import { Injectable } from '@nestjs/common';
import { DatabaseService } from '../../database/database.service';
import {
  UserDataExportBlockedUserResponse,
  UserDataExportFriendResponse,
  UserDataExportFriendState,
  UserDataExportProfileResponse,
} from '../users.types';

interface ExportProfileRow {
  username: string;
  display_name: string | null;
  avatar_url: string | null;
  banner_url: string | null;
  bio: string | null;
  status: UserDataExportProfileResponse['status'];
  last_seen_at: Date | null;
  created_at: Date;
}

interface ExportFriendRow {
  username: string;
  state: UserDataExportFriendState;
  since: Date;
}

interface ExportBlockedUserRow {
  username: string;
  blocked_at: Date;
}

interface DataExportJobRow {
  id: string;
  status: 'pending' | 'ready' | 'failed';
  object_key: string | null;
  expires_at: Date | null;
  created_at: Date;
  updated_at: Date;
}

@Injectable()
export class DataExportRepository {
  constructor(private readonly database: DatabaseService) {}

  async getProfileExportById(
    userId: string,
  ): Promise<UserDataExportProfileResponse | null> {
    const result = await this.database.client.query<ExportProfileRow>(
      `
        SELECT
          username,
          display_name,
          avatar_url,
          banner_url,
          bio,
          status,
          last_seen_at,
          created_at
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

    return {
      username: row.username,
      display_name: row.display_name,
      avatar_url: row.avatar_url,
      banner_url: row.banner_url,
      bio: row.bio,
      status: row.status,
      last_seen_at: row.last_seen_at ? row.last_seen_at.toISOString() : null,
      created_at: row.created_at.toISOString(),
    };
  }

  async listFriendExports(userId: string): Promise<UserDataExportFriendResponse[]> {
    const accepted = await this.database.client.query<{
      username: string;
      since: Date;
    }>(
      `
        SELECT
          profile.username,
          friendship.created_at AS since
        FROM friendships AS friendship
        JOIN users_profile AS profile
          ON profile.id = CASE
            WHEN friendship.requester_id = $1::bigint THEN friendship.addressee_id
            ELSE friendship.requester_id
          END
        WHERE friendship.status = 'accepted'
          AND (friendship.requester_id = $1::bigint OR friendship.addressee_id = $1::bigint)
        ORDER BY friendship.created_at DESC, friendship.id DESC
      `,
      [userId],
    );

    const pending = await this.database.client.query<ExportFriendRow>(
      `
        SELECT
          profile.username,
          CASE
            WHEN friendship.requester_id = $1::bigint THEN 'pending_outgoing'
            ELSE 'pending_incoming'
          END AS state,
          friendship.created_at AS since
        FROM friendships AS friendship
        JOIN users_profile AS profile
          ON profile.id = CASE
            WHEN friendship.requester_id = $1::bigint THEN friendship.addressee_id
            ELSE friendship.requester_id
          END
        WHERE friendship.status = 'pending'
          AND (friendship.requester_id = $1::bigint OR friendship.addressee_id = $1::bigint)
        ORDER BY friendship.created_at DESC, friendship.id DESC
      `,
      [userId],
    );

    const acceptedRows = accepted.rows.map((row) => ({
      username: row.username,
      state: 'accepted' as const,
      since: row.since.toISOString(),
    }));

    const pendingRows = pending.rows.map((row) => ({
      username: row.username,
      state: row.state,
      since: row.since.toISOString(),
    }));

    return [...acceptedRows, ...pendingRows];
  }

  async listBlockedExportUsers(
    userId: string,
  ): Promise<UserDataExportBlockedUserResponse[]> {
    const result = await this.database.client.query<ExportBlockedUserRow>(
      `
        SELECT
          profile.username,
          block.created_at AS blocked_at
        FROM user_blocks AS block
        JOIN users_profile AS profile
          ON profile.id = block.blocked_id
        WHERE block.blocker_id = $1::bigint
        ORDER BY block.created_at DESC, profile.username ASC, profile.id ASC
      `,
      [userId],
    );

    return result.rows.map((row) => ({
      username: row.username,
      blocked_at: row.blocked_at.toISOString(),
    }));
  }

  async getLatestPendingExport(userId: string): Promise<DataExportJobRow | null> {
    const result = await this.database.client.query<DataExportJobRow>(
      `
        SELECT
          id::text,
          status,
          object_key,
          expires_at,
          created_at,
          updated_at
        FROM data_exports
        WHERE user_id = $1::bigint
          AND status = 'pending'
        ORDER BY created_at DESC, id DESC
        LIMIT 1
      `,
      [userId],
    );

    return result.rows[0] ?? null;
  }

  async createPendingExport(
    exportId: string,
    userId: string,
  ): Promise<DataExportJobRow | 'conflict'> {
    try {
      const result = await this.database.client.query<DataExportJobRow>(
        `
          INSERT INTO data_exports (id, user_id, status)
          VALUES ($1::bigint, $2::bigint, 'pending')
          RETURNING
            id::text,
            status,
            object_key,
            expires_at,
            created_at,
            updated_at
        `,
        [exportId, userId],
      );

      return result.rows[0] ?? 'conflict';
    } catch (error) {
      if (
        typeof error === 'object' &&
        error !== null &&
        'code' in error &&
        (error as { code?: string }).code === '23505'
      ) {
        return 'conflict';
      }

      throw error;
    }
  }

  async getExportById(
    userId: string,
    exportId: string,
  ): Promise<DataExportJobRow | null> {
    const result = await this.database.client.query<DataExportJobRow>(
      `
        SELECT
          id::text,
          status,
          object_key,
          expires_at,
          created_at,
          updated_at
        FROM data_exports
        WHERE user_id = $1::bigint
          AND id = $2::bigint
        LIMIT 1
      `,
      [userId, exportId],
    );

    return result.rows[0] ?? null;
  }

  async markExportReady(
    exportId: string,
    objectKey: string,
    expiresAt: Date,
  ): Promise<boolean> {
    const result = await this.database.client.query(
      `
        UPDATE data_exports
        SET status = 'ready',
            object_key = $2,
            expires_at = $3,
            updated_at = NOW()
        WHERE id = $1::bigint
      `,
      [exportId, objectKey, expiresAt],
    );

    return (result.rowCount ?? 0) > 0;
  }

  async markExportFailed(exportId: string): Promise<boolean> {
    const result = await this.database.client.query(
      `
        UPDATE data_exports
        SET status = 'failed',
            updated_at = NOW()
        WHERE id = $1::bigint
      `,
      [exportId],
    );

    return (result.rowCount ?? 0) > 0;
  }
}
