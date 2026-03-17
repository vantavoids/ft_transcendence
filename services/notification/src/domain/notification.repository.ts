export interface INotificationRepository {
	getNotif(userId: string): Promise<Notification[]>;
	readNotif(notifId: string): Promise<Notification>;
	readAll(userId: string): Promise<number>;
}