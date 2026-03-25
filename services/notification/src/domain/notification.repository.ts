import { NotificationEntity } from "./notification.entity";

export interface INotificationRepository {
	get(userId: string): Promise<NotificationEntity[]>;
	read(notifId: string): Promise<NotificationEntity>;
	readAll(userId: string): Promise<number>;
}