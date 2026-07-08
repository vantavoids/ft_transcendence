import { Injectable } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import {
  DeleteObjectCommand,
  PutObjectCommand,
  S3Client,
} from '@aws-sdk/client-s3';
import { ValidatedEnv } from '../../config/env';

export interface UploadFile {
  buffer: Buffer;
  mimetype: string;
}

export type ProfileMediaKind = 'avatar' | 'banner';

@Injectable()
export class ProfileMediaStorageService {
  private readonly client: S3Client;
  private readonly baseUrl: string;
  private readonly bucket: string;

  constructor(private readonly config: ConfigService<ValidatedEnv, true>) {
    this.client = new S3Client({
      region: 'us-east-1',
      endpoint: this.config.getOrThrow('MINIO_ENDPOINT'),
      forcePathStyle: true,
      credentials: {
        accessKeyId: this.config.getOrThrow('MINIO_ACCESS_KEY'),
        secretAccessKey: this.config.getOrThrow('MINIO_SECRET_KEY'),
      },
    });
    this.baseUrl = this.config.getOrThrow('BASE_URL').replace(/\/$/, '');
    this.bucket = this.config.getOrThrow('MINIO_USER_BUCKET');
  }

  async upload(
    kind: ProfileMediaKind,
    userId: string,
    uploadId: string,
    file: UploadFile,
  ): Promise<string> {
    const key = this.buildKey(kind, userId, uploadId);
    await this.client.send(
      new PutObjectCommand({
        Bucket: this.bucket,
        Key: key,
        Body: file.buffer,
        ContentType: file.mimetype,
      }),
    );

    return this.buildPublicUrl(key);
  }

  async delete(kind: ProfileMediaKind, userId: string, uploadId: string): Promise<void> {
    const key = this.buildKey(kind, userId, uploadId);
    await this.client.send(
      new DeleteObjectCommand({
        Bucket: this.bucket,
        Key: key,
      }),
    );
  }

  extractKeyFromUrl(url: string, kind: ProfileMediaKind, userId: string): string | null {
    try {
      const parsed = new URL(url);
      const prefix = `/s3/${this.bucket}/${this.buildKeyPrefix(kind, userId)}`;
      if (!parsed.pathname.startsWith(prefix)) {
        return null;
      }

      return decodeURIComponent(parsed.pathname.slice(`/s3/${this.bucket}/`.length));
    } catch {
      return null;
    }
  }

  private buildKey(kind: ProfileMediaKind, userId: string, uploadId: string): string {
    return `${this.buildKeyPrefix(kind, userId)}${uploadId}`;
  }

  private buildKeyPrefix(kind: ProfileMediaKind, userId: string): string {
    return `${kind}s/${userId}/`;
  }

  private buildPublicUrl(key: string): string {
    return `${this.baseUrl}/s3/${this.bucket}/${key}`;
  }
}
