import { Module } from '@nestjs/common';
import { NotificationController } from './presentation/notification.controller';
import { NotificationService } from './application/notification.service';

@Module({
  controllers: [NotificationController],
  providers: [
    NotificationService
  ]
})
export class NotificationModule {}