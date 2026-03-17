import { Injectable } from '@nestjs/common';
import type { INotificationRepository } from 'src/domain/notification.repository';

@Injectable()
export class NotificationService {
	constructor(private repo: INotificationRepository) {}

	getNotif(userId: string): Promise<Notification[]> {
		return this.repo.getNotif(userId);
	}

	readNotif(notifId: string): Promise<Notification> {
		return this.repo.readNotif(notifId);
	}

	readAll(userId: string): Promise<number> {
		return this.repo.readAll(userId);
	}
}
