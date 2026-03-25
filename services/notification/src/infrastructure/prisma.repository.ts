import type { INotificationRepository } from "src/domain/notification.repository";
import { NotificationEntity } from "src/domain/notification.entity";

export class PrismaNotificationRepository implements INotificationRepository
{
	get(userId: string): Promise<NotificationEntity[]> {
		return Promise.resolve([]);
	}

	read(notifId: string): Promise<NotificationEntity> {
		throw new Error('Not implemented yet');
	}

	readAll(userId: string): Promise<number> {
		return Promise.resolve(0);
	}
}