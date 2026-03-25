import { Injectable } from '@nestjs/common';
import type { INotificationRepository } from 'src/domain/notification.repository';
import { NotificationEntity } from "src/domain/notification.entity";

@Injectable()
export class NotificationService {
	constructor(private repo: INotificationRepository) {}

	get(userId: string): Promise<NotificationEntity[]> {
		return this.repo.get(userId);
	}

	read(notifId: string): Promise<NotificationEntity> {
		return this.repo.read(notifId);
	}

	readAll(userId: string): Promise<number> {
		return this.repo.readAll(userId);
	}
}
