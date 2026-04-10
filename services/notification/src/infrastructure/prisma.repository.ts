import { ForbiddenException, NotFoundException } from '@nestjs/common';
import { PrismaClient } from '../../generated/prisma/client.js';
import type { NotificationData } from '../domain/notification.data.js';
import type { INotificationRepository } from '../domain/notification.repository.js';

export class PrismaNotificationRepository implements INotificationRepository {
  constructor(private readonly prisma: PrismaClient) {}

  async get(uid: bigint, read?: boolean, limit?: number, before?: bigint): Promise<NotificationData[]> {
    let cursorDate: Date | undefined;

    if (before) {
      const cursor = await this.prisma.notificationData.findUnique({
        where: { id: BigInt(before) }
      });
      cursorDate = cursor?.created_at;
    }
  
    return this.prisma.notificationData.findMany({
      where: {
        user_id: uid,
        read: read,
        ...(cursorDate && { created_at: { lt: cursorDate} }),
      },
      take: limit ?? 50,
      orderBy: { created_at: 'desc' },
    }) as unknown as Promise<NotificationData[]> ;
  }

  async read(uid: bigint, id:bigint): Promise<NotificationData> {
    const find = await this.prisma.notificationData.findUnique({
      where: { id: id }
    });
    
    if (!find) throw new NotFoundException();
    if (find.user_id !== uid) throw new ForbiddenException();

    const notif = await this.prisma.notificationData.update({
      where: { id: id },
      data: { read: true },
    });

    return notif as unknown as Promise<NotificationData>;
  }

  async readAll(uid: bigint): Promise<{ updated: number }> {
    const readed = await this.prisma.notificationData.updateMany({
      where: {
        user_id: uid,
        read: false,
      },
      data: {read: true},
    });
  
    return { updated: readed.count };
  }
}