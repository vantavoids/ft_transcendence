import { NotificationType } from '../../generated/prisma/enums.js';

export type MentionPayload = {
  channel_id: string;
  guild_id: string;
  author_id: string;
  preview: string;
};

export type DmPayload = {
  sender_id: string;
  preview: string;
};

export type FriendRequestPayload = {
  from_user_id: string;
};

export type GuildInvitePayload = {
  guild_id: string;
  guild_name: string;
  from_user_id: string;
};

export type GuildWelcomePayload = {
  guild_id: string;
  guild_name: string;
};

export type IncomingCallPayload = {
  call_type: 'audio' | 'video';
};

export type NotificationPayload = 
  | MentionPayload
  | DmPayload
  | FriendRequestPayload
  | GuildInvitePayload
  | GuildWelcomePayload
  | IncomingCallPayload;

export interface NotificationData {
  id: bigint;
  user_id: bigint;
  type: NotificationType;
  actor_id: bigint | null;
  source_id: bigint | null;
  payload: NotificationPayload;
  read: boolean;
  dismissed_at: Date | null;
  expires_at: Date | null;
  created_at: Date;
}
