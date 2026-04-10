import { Module } from '@nestjs/common';
import { NotificationController } from './presentation/notification.controller.js';
import { NotificationService } from './application/notification.service.js';
import { PrismaNotificationRepository } from './infrastructure/prisma.repository.js';
import { PrismaClient } from '../generated/prisma/client.js';

@Module({
  controllers: [NotificationController],
  providers: [
    NotificationService,
    PrismaClient,
    {
      provide: 'INotificationRepository',
      useClass: PrismaNotificationRepository,
    },
  ],
})
export class NotificationModule {}