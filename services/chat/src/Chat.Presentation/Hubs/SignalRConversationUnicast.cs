using Chat.Application.Abstractions;
using Chat.Application.Features.DirectMessages.Common;
using Microsoft.AspNetCore.SignalR;

namespace Chat.Presentation.Hubs;

internal sealed class SignalRConversationUnicast(IHubContext<ChatHub, IChatClient> hub)
	: IConversationUnicast
{
	public Task UnicastMessageAsync(DirectMessageResponse message, CancellationToken ct)
	{
		return hub.Clients
			.Users([message.SenderId, message.RecipientId])
			.ReceiveDirectMessage(message);
	}

	public Task UnicastMessageEditedAsync(long senderId, long recipientId, DirectMessageEditedEvent evt, CancellationToken ct) =>
		hub.Clients.Users([senderId.ToString(), recipientId.ToString()]).DirectMessageEdited(evt);

	public Task UnicastMessageDeletedAsync(long senderId, long recipientId, DirectMessageDeletedEvent evt, CancellationToken ct) =>
		hub.Clients.Users([senderId.ToString(), recipientId.ToString()]).DirectMessageDeleted(evt);
}
