import { NotificationData } from "./notification.data";

export interface INotificationRepository {
  get(uid: bigint, read?: boolean, limit?: number, before?: bigint): Promise<NotificationData[]>;
  read(uid: bigint, id:bigint): Promise<NotificationData>;
  readAll(uid: bigint): Promise<{ updated: number }>;
}