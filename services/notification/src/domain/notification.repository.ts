import { PrismaPromise } from "../../generated/prisma/internal/prismaNamespace";
import { NotificationData } from "./notification.data";

export interface INotificationRepository {
  get(uid: bigint, read?: boolean, limit?: number, before?: bigint): Promise<NotificationData[]>;
  readAll(uid: bigint): Promise<{ updated: number }>;
  read(uid: bigint, id:bigint): Promise<NotificationData>;
  delete(uid: bigint, id: bigint): Promise<void>;
}