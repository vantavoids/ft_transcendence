using Chat.Application.Features.Channels.Common;
using Chat.Application.Features.DirectMessages.Common;

namespace Chat.Presentation.Hubs;

public interface IChatClient
{
	Task ReceiveMessage(ChannelMessageResponse message);
	Task ReceiveDirectMessage(DirectMessageResponse message);

	Task MessageEdited(ChannelMessageEditedEvent evt);
	Task MessageDeleted(ChannelMessageDeletedEvent evt);

	Task DirectMessageEdited(DirectMessageEditedEvent evt);
	Task DirectMessageDeleted(DirectMessageDeletedEvent evt);

	Task TypingStarted(string userId, string scope, string id, DateTimeOffset expiresAt);

	Task GuildJoined(string guildId, string guildName);
	Task GuildLeft(string guildId);
	Task Error(string code, string message);
}
