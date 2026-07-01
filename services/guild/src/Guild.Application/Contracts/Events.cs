namespace Guild.Application.Contracts;

public sealed record GuildMemberJoined(long GuildId, string GuildName, long UserId);
public sealed record GuildMemberLeft(long GuildId, long UserId);
public sealed record GuildInviteCreated(long GuildId, string GuildName, long InvitedByUserId, long? InvitedUserId);
public sealed record GuildOwnerTransferred(long GuildId, long OldOwnerId, long NewOwnerId);
