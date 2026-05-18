using Chat.Application.Features.Messages.Common;

namespace Chat.Presentation.Hubs;

public interface IChatClient
{
	Task ReceiveMessage(MessageResponse message);
	Task Error(string code, string message);
}
