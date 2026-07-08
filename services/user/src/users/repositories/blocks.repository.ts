import { Injectable } from '@nestjs/common';
import { DatabaseService } from '../../database/database.service';
import { BlockListItemResponse } from '../users.types';

interface BlockedUserRow {
  id: string;
  username: string;
  blocked_at: Date;
}

@Injectable()
export class BlocksRepository {
  constructor(private readonly database: DatabaseService) {}

  async listBlockedUsers(viewerId: string): Promise<BlockListItemResponse[]> {
    const result = await this.database.client.query<BlockedUserRow>(
      `
        SELECT
          profile.id::text,
          profile.username,
          block.created_at AS blocked_at
        FROM user_blocks AS block
        JOIN users_profile AS profile
          ON profile.id = block.blocked_id
        WHERE block.blocker_id = $1::bigint
        ORDER BY block.created_at DESC, profile.username ASC, profile.id ASC
      `,
      [viewerId],
    );

    return result.rows.map((row) => this.toBlockedUser(row));
  }

  async blockUser(
    viewerId: string,
    blockedId: string,
  ): Promise<'not_found' | 'conflict' | 'blocked'> {
    const result = await this.database.client.query<{ outcome: string }>(
      `
        WITH target AS (
          SELECT id
          FROM users_profile
          WHERE id = $2::bigint
          LIMIT 1
        ),
        existing_block AS (
          SELECT 1
          FROM user_blocks
          WHERE blocker_id = $1::bigint
            AND blocked_id = $2::bigint
          LIMIT 1
        ),
        deleted_friendships AS (
          DELETE FROM friendships
          WHERE LEAST(requester_id, addressee_id) = LEAST($1::bigint, $2::bigint)
            AND GREATEST(requester_id, addressee_id) = GREATEST($1::bigint, $2::bigint)
            AND EXISTS (SELECT 1 FROM target)
            AND NOT EXISTS (SELECT 1 FROM existing_block)
          RETURNING id
        ),
        inserted_block AS (
          INSERT INTO user_blocks (blocker_id, blocked_id)
          SELECT $1::bigint, $2::bigint
          WHERE EXISTS (SELECT 1 FROM target)
            AND NOT EXISTS (SELECT 1 FROM existing_block)
          RETURNING blocker_id
        )
        SELECT
          CASE
            WHEN NOT EXISTS (SELECT 1 FROM target) THEN 'not_found'
            WHEN EXISTS (SELECT 1 FROM existing_block) THEN 'conflict'
            WHEN EXISTS (SELECT 1 FROM inserted_block) THEN 'blocked'
            ELSE 'conflict'
          END AS outcome
      `,
      [viewerId, blockedId],
    );

    return result.rows[0]?.outcome as 'not_found' | 'conflict' | 'blocked';
  }

  async unblockUser(
    viewerId: string,
    blockedId: string,
  ): Promise<'not_found' | 'deleted'> {
    const result = await this.database.client.query<{ created_at: Date }>(
      `
        DELETE FROM user_blocks
        WHERE blocker_id = $1::bigint
          AND blocked_id = $2::bigint
        RETURNING created_at
      `,
      [viewerId, blockedId],
    );

    if (result.rowCount === 0) {
      return 'not_found';
    }

    return 'deleted';
  }

  private toBlockedUser(row: BlockedUserRow): BlockListItemResponse {
    return {
      id: row.id,
      username: row.username,
      blocked_at: row.blocked_at.toISOString(),
    };
  }
}
