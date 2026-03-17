import { INotificationRepository } from "src/domain/notification.repository";

export class PrismaNotificationRepository implements INotificationRepository
{
	getNotif(userId: string): Promise<Notification[]> {
		return new ;
	}

	readNotif(notifId: string): Promise<Notification> {
		
	}

	readAll(userId: string): Promise<number> {
		
	}
}