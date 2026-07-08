import { Injectable } from '@nestjs/common';
import { DatabaseService } from '../../database/database.service';

interface ProfileMediaRow {
  avatar_url: string | null;
  banner_url: string | null;
}

@Injectable()
export class ProfileMediaRepository {
  constructor(private readonly database: DatabaseService) {}

  async getMediaById(userId: string): Promise<ProfileMediaRow | null> {
    const result = await this.database.client.query<ProfileMediaRow>(
      `
        SELECT
          avatar_url,
          banner_url
        FROM users_profile
        WHERE id = $1::bigint
        LIMIT 1
      `,
      [userId],
    );

    return result.rows[0] ?? null;
  }

  async setAvatarUrl(userId: string, avatarUrl: string): Promise<boolean> {
    const result = await this.database.client.query(
      `
        UPDATE users_profile
        SET avatar_url = $2,
            updated_at = NOW()
        WHERE id = $1::bigint
      `,
      [userId, avatarUrl],
    );

    return (result.rowCount ?? 0) > 0;
  }

  async clearAvatarUrl(userId: string): Promise<boolean> {
    const result = await this.database.client.query(
      `
        UPDATE users_profile
        SET avatar_url = NULL,
            updated_at = NOW()
        WHERE id = $1::bigint
          AND avatar_url IS NOT NULL
      `,
      [userId],
    );

    return (result.rowCount ?? 0) > 0;
  }

  async setBannerUrl(userId: string, bannerUrl: string): Promise<boolean> {
    const result = await this.database.client.query(
      `
        UPDATE users_profile
        SET banner_url = $2,
            updated_at = NOW()
        WHERE id = $1::bigint
      `,
      [userId, bannerUrl],
    );

    return (result.rowCount ?? 0) > 0;
  }

  async clearBannerUrl(userId: string): Promise<boolean> {
    const result = await this.database.client.query(
      `
        UPDATE users_profile
        SET banner_url = NULL,
            updated_at = NOW()
        WHERE id = $1::bigint
          AND banner_url IS NOT NULL
      `,
      [userId],
    );

    return (result.rowCount ?? 0) > 0;
  }
}
