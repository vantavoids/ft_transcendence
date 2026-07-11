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
  // internal client: reaches MinIO by service name for uploads
  private readonly client: S3Client;
  // public client: only used to presign download URLs. It signs against the
  // browser-reachable endpoint (nginx :9443 -> MinIO) so the resulting SigV4
  // URL validates when a user opens it, instead of baking in the internal
  // `minio:9000` host which a browser can't resolve.
  private readonly presignClient: S3Client;
  private readonly bucket: string;

  constructor(private readonly config: ConfigService<ValidatedEnv, true>) {
    const credentials = {
      accessKeyId: this.config.getOrThrow('MINIO_ACCESS_KEY'),
      secretAccessKey: this.config.getOrThrow('MINIO_SECRET_KEY'),
    };
    this.client = new S3Client({
      region: 'us-east-1',
      endpoint: this.config.getOrThrow('MINIO_ENDPOINT'),
      forcePathStyle: true,
      credentials,
    });
    this.presignClient = new S3Client({
      region: 'us-east-1',
      endpoint: this.config.getOrThrow('MINIO_PUBLIC_ENDPOINT'),
      forcePathStyle: true,
      credentials,
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
      this.presignClient,
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
