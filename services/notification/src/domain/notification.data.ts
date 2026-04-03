export enum NotificationType {
  MENTION = 'mention',
  DM = 'dm',
  FRIEND_REQUEST = 'friend_request',
  GUILD_INVITE = 'guild_invite',
  GUILD_WELCOME = 'guild_welcome',
  INCOMING_CALL = 'incoming_call',
}

export class NotificationPayload {
  channel_id!: string;
  guild_id!: string;
  author_id!: string;
  preview!: string;
}

export class NotificationData {
  id!: bigint;
  user_id!: bigint;
  type!: NotificationType;
  actor_id?: bigint;
  source_id?: bigint;
  payload!: NotificationPayload;
  read!: boolean;
  dismissed_at?: Date;
  expires_at?: Date;
  created_at!: Date;
}
