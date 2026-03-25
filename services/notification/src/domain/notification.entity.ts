export enum NotificationType {
    MENTION = 'mention',
    DM = 'dm',
    FRIEND_REQUEST = 'friend_request',
    GUILD_INVITE = 'guild_invite',
}

export class MentionPayload {
    channel_id: string;
    guild_id: string;
    author_id: string;
    preview: string;
}

export class DmPayload {
    sender_id: string;
    preview: string;
}

export class FriendRequestPayload {
    from_user_id: string;
}

export class GuildInvitePayload {
    guild_id: string;
    guild_name: string;
    from_user_id: string;
}

export type NotificationPayload = MentionPayload | DmPayload | FriendRequestPayload | GuildInvitePayload;

export class NotificationEntity {
    id: string;
    userId: string;
    type: NotificationType;
    payload: NotificationPayload;
    read: boolean;
    created_at: string;
}
