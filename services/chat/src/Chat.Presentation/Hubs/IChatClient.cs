using Chat.Application.Features.Messages.Common;

namespace Chat.Presentation.Hubs;

public interface IChatClient
{
	Task ReceiveMessage(MessageResponse message);
	Task GuildJoined(string guildId, string guildName);
	Task Error(string code, string message);
}
