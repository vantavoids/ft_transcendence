import { Injectable } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import {
  GetObjectCommand,
  PutObjectCommand,
  S3Client,
} from '@aws-sdk/client-s3';
import { getSignedUrl } from '@aws-sdk/s3-request-presigner';
import { ValidatedEnv } from '../config/env';

@Injectable()
export class DataExportStorageService {
  private readonly client: S3Client;
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
    this.bucket = this.config.getOrThrow('MINIO_EXPORTS_BUCKET');
  }

  buildObjectKey(userId: string, exportId: string): string {
    return `exports/${userId}/${exportId}.json`;
  }

  async storeBundle(objectKey: string, body: string): Promise<void> {
    await this.client.send(
      new PutObjectCommand({
        Bucket: this.bucket,
        Key: objectKey,
        Body: body,
        ContentType: 'application/json',
      }),
    );
  }

  async createDownloadUrl(
    objectKey: string,
    expiresInSeconds: number,
  ): Promise<string> {
    return getSignedUrl(
      this.client,
      new GetObjectCommand({
        Bucket: this.bucket,
        Key: objectKey,
      }),
      {
        expiresIn: Math.max(1, Math.floor(expiresInSeconds)),
      },
    );
  }
}
