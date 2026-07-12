using Chat.Application.Features.Channels.Common;
using Chat.Application.Features.DirectMessages.Common;

namespace Chat.Presentation.Hubs;

public interface IChatClient
{
	Task ReceiveMessage(ChannelMessageResponse message);
	Task ReceiveDirectMessage(DirectMessageResponse message);

	Task MessageEdited(ChannelMessageEditedEvent evt);
	Task MessageDeleted(ChannelMessageDeletedEvent evt);

	Task ReactionAdded(ReactionAddedEvent evt);
	Task ReactionRemoved(ReactionRemovedEvent evt);

	Task DirectMessageEdited(DirectMessageEditedEvent evt);
	Task DirectMessageDeleted(DirectMessageDeletedEvent evt);

	Task TypingStarted(string userId, string scope, string id, DateTimeOffset expiresAt);

	Task ReadStateUpdated(ChannelReadStateResponse response);
	Task DmReadStateUpdated(DmReadStateResponse response);

	Task GuildJoined(string guildId, string guildName);
	Task GuildLeft(string guildId);
	Task GuildDeleted(string guildId);
	Task GuildUpdated(GuildUpdatedEvent evt);

	Task ChannelCreated(GuildChannelDto channel);
	Task ChannelUpdated(GuildChannelDto channel);
	Task ChannelDeleted(ChannelDeletedEvent evt);

	Task CategoryCreated(GuildCategoryDto category);
	Task CategoryUpdated(GuildCategoryDto category);
	Task CategoryDeleted(CategoryDeletedEvent evt);

	Task MemberJoined(GuildMemberEvent evt);
	Task MemberLeft(GuildMemberEvent evt);
	Task MemberUpdated(GuildMemberEvent evt);

	Task RolesChanged(string guildId);
	Task ChannelAccessGranted(ChannelAccessGrantedEvent evt);

	Task UserPresence(UserPresenceEvent evt);

	Task Error(string code, string message);
}

public sealed record ChannelDeletedEvent(string GuildId, string ChannelId);
public sealed record CategoryDeletedEvent(string GuildId, string CategoryId);
public sealed record GuildMemberEvent(string GuildId, string UserId);
public sealed record UserPresenceEvent(string UserId, string Status);
public sealed record GuildUpdatedEvent(string GuildId, string Name, string? IconUrl);
public sealed record ChannelAccessGrantedEvent(string GuildId, string ChannelId);
