import { Injectable, Logger } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { SnowflakeIdGenerator } from '../common/snowflake-id.generator';
import { ValidatedEnv } from '../config/env';
import { UsersService } from './users.service';
import { DataExportRepository } from './repositories/data-export.repository';
import { DataExportBundle, CreateDataExportResponse, DataExportReadyEvent, DataExportStatusResponse } from './data-export.types';
import { DataExportEventsPublisher } from './data-export.publisher';
import { DataExportStorageService } from './data-export.storage';

interface FetchResult<T> {
  value?: T;
  error?: string;
}

interface AuthExportResponse {
  user_id: string;
  email: string | null;
}

@Injectable()
export class DataExportService {
  private static readonly exportTtlSeconds = 7 * 24 * 60 * 60;
  private static readonly requestTimeoutMs = 5_000;
  private readonly logger = new Logger(DataExportService.name);

  constructor(
    private readonly users: UsersService,
    private readonly repository: DataExportRepository,
    private readonly storage: DataExportStorageService,
    private readonly publisher: DataExportEventsPublisher,
    private readonly snowflakeIdGenerator: SnowflakeIdGenerator,
    private readonly config: ConfigService<ValidatedEnv, true>,
  ) {}

  async requestExport(userId: string): Promise<CreateDataExportResponse> {
    const existing = await this.repository.getLatestPendingExport(userId);
    if (existing) {
      return { export_id: existing.id, status: 'pending' };
    }

    const exportId = this.snowflakeIdGenerator.nextId();
    const created = await this.repository.createPendingExport(exportId, userId);
    const jobId = created === 'conflict' ? (await this.repository.getLatestPendingExport(userId))?.id : created.id;
    if (!jobId) {
      return { export_id: exportId, status: 'pending' };
    }

    void this.processExportJob(jobId, userId).catch((error) => {
      this.logger.error(`data export job ${jobId} failed`, error as Error);
    });

    return { export_id: jobId, status: 'pending' };
  }

  async getExportStatus(
    userId: string,
    exportId: string,
  ): Promise<DataExportStatusResponse | null> {
    const job = await this.repository.getExportById(userId, exportId);
    if (!job) {
      return null;
    }

    if (job.status === 'pending') {
      return { export_id: job.id, status: 'pending' };
    }

    if (job.status === 'failed' || !job.object_key || !job.expires_at) {
      return {
        export_id: job.id,
        status: 'failed',
        error: 'bundle upload to object storage failed',
      };
    }

    const secondsRemaining = Math.max(
      1,
      Math.floor((job.expires_at.getTime() - Date.now()) / 1000),
    );

    return {
      export_id: job.id,
      status: 'ready',
      download_url: await this.storage.createDownloadUrl(
        job.object_key,
        secondsRemaining,
      ),
      expires_at: job.expires_at.toISOString(),
    };
  }

  private async processExportJob(
    exportId: string,
    userId: string,
  ): Promise<void> {
    try {
      const { bundle, email } = await this.buildBundle(exportId, userId);
      const objectKey = this.storage.buildObjectKey(userId, exportId);
      const body = JSON.stringify(bundle, null, 2);
      await this.storage.storeBundle(objectKey, body);

      const expiresAt = new Date(
        Date.now() + DataExportService.exportTtlSeconds * 1000,
      );
      const updated = await this.repository.markExportReady(
        exportId,
        objectKey,
        expiresAt,
      );
      if (!updated) {
        throw new Error('could not mark export as ready');
      }

      if (email) {
        try {
          const downloadUrl = await this.storage.createDownloadUrl(
            objectKey,
            DataExportService.exportTtlSeconds,
          );
          await this.publisher.publishDataExportReady({
            user_id: userId,
            email,
            download_url: downloadUrl,
            expires_at: expiresAt.toISOString(),
          } satisfies DataExportReadyEvent);
        } catch (error) {
          this.logger.error(
            `failed to publish export-ready event for ${exportId}`,
            error instanceof Error ? error.stack : undefined,
          );
        }
      }
    } catch (error) {
      this.logger.error(
        `failed to assemble export ${exportId} for user ${userId}`,
        error instanceof Error ? error.stack : undefined,
      );
      await this.repository.markExportFailed(exportId).catch(() => undefined);
    }
  }

  private async buildBundle(
    exportId: string,
    userId: string,
  ): Promise<{ bundle: DataExportBundle; email: string | null }> {
    const generatedAt = new Date().toISOString();
    const errors: Record<string, string> = {};
    const services: DataExportBundle['services'] = {};

    const auth = await this.fetchJson<AuthExportResponse>(
      this.resolveUrl('AUTH_INTERNAL_URL', `/internal/users/${userId}/data-export`),
    );
    if (auth.value) {
      services.auth = auth.value;
    } else if (auth.error) {
      errors.auth = auth.error;
    }

    const user = await this.safeUserExport(userId);
    services.user = user;

    const guild = await this.fetchJson<unknown>(
      this.resolveUrl('GUILD_INTERNAL_URL', `/internal/users/${userId}/data-export`),
    );
    if (guild.value) {
      services.guild = guild.value;
    } else if (guild.error) {
      errors.guild = guild.error;
    }

    const chat = await this.fetchJson<unknown>(
      this.resolveUrl('CHAT_INTERNAL_URL', `/internal/users/${userId}/data-export`),
    );
    if (chat.value) {
      services.chat = chat.value;
    } else if (chat.error) {
      errors.chat = chat.error;
    }

    const notification = await this.fetchJson<unknown>(
      this.resolveUrl(
        'NOTIFICATION_INTERNAL_URL',
        `/internal/users/${userId}/data-export`,
      ),
    );
    if (notification.value) {
      services.notification = notification.value;
    } else if (notification.error) {
      errors.notification = notification.error;
    }

    const bundle: DataExportBundle = {
      export_id: exportId,
      user_id: userId,
      generated_at: generatedAt,
      services,
    };
    if (Object.keys(errors).length > 0) {
      bundle.errors = errors;
    }

    return { bundle, email: this.extractEmail(auth.value) };
  }

  private async safeUserExport(userId: string): Promise<DataExportBundle['services']['user']> {
    return this.users.getInternalDataExport(userId);
  }

  private extractEmail(value: AuthExportResponse | undefined): string | null {
    if (!value || typeof value.email !== 'string' || value.email.trim() === '') {
      return null;
    }

    return value.email;
  }

  private async fetchJson<T>(url: string): Promise<FetchResult<T>> {
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), DataExportService.requestTimeoutMs);

    try {
      const response = await fetch(url, {
        method: 'GET',
        headers: {
          Accept: 'application/json',
        },
        signal: controller.signal,
      });

      if (!response.ok) {
        const body = await response.text().catch(() => '');
        return {
          error: body
            ? `${response.status} ${body}`
            : `http ${response.status}`,
        };
      }

      return {
        value: (await response.json()) as T,
      };
    } catch (error) {
      return {
        error:
          error instanceof DOMException && error.name === 'AbortError'
            ? `timeout after ${DataExportService.requestTimeoutMs / 1000}s`
            : 'unreachable',
      };
    } finally {
      clearTimeout(timeout);
    }
  }

  private resolveUrl(envKey: keyof ValidatedEnv, path: string): string {
    return `${this.config.getOrThrow(envKey).replace(/\/$/, '')}${path}`;
  }
}
