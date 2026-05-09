namespace Chat.Presentation.Hubs;

public interface IChatClient
{
	Task ReceiveMessage(string message);
}
