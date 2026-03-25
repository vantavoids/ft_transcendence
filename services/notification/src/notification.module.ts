import { Module } from '@nestjs/common';
import { NotificationController } from './presentation/notification.controller';
import { NotificationService } from './application/notification.service';
import { PrismaNotificationRepository } from './infrastructure/prisma.repository';

@Module({
  controllers: [NotificationController],
  providers: [
    NotificationService,
    {
      provide: 'INotificationRepository',
      useClass: PrismaNotificationRepository,
    }
  ]
})
export class NotificationModule {}