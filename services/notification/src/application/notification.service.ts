import { Inject, Injectable } from '@nestjs/common';
import type { NotificationData } from '../domain/notification.data.js';
import type { INotificationRepository } from '../domain/notification.repository.js';

@Injectable()
export class NotificationService {
  constructor(
    @Inject('INotificationRepository')
    private readonly repo: INotificationRepository,
  ) {}

  async get(uid: bigint, read?: boolean, limit?: number, before?: bigint): Promise<NotificationData[]> {
    return this.repo.get(uid, read, limit, before);
  }

  async read(uid: bigint, id:bigint): Promise<NotificationData> {
    return this.repo.read(uid, id);
  }

  async readAll(uid: bigint): Promise<{ updated: number }> {
    return this.repo.readAll(uid);
  }
}
