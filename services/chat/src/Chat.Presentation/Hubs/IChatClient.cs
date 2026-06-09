using Chat.Application.Features.Messages.Common;

namespace Chat.Presentation.Hubs;

public interface IChatClient
{
	Task ReceiveMessage(MessageResponse message);
	Task MessageEdited(MessageEditedEvent evt);
	Task MessageDeleted(MessageDeletedEvent evt);
	Task GuildJoined(string guildId, string guildName);
	Task GuildLeft(string guildId);
	Task Error(string code, string message);
}
