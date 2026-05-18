using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.Messages.Common;
using Chat.Application.Features.Messages.SendMessage;
using Chat.Domain.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Chat.Presentation.Hubs;

[Authorize]
public sealed class ChatHub(
	ICommandHandler<SendMessageCommand, Result<MessageResponse>> sendHandler)
	: Hub<IChatClient>
{
	public async Task SendMessage(SendMessagePayload payload)
	{
		var result = await sendHandler.HandleAsync(
			new SendMessageCommand(payload.ChannelId, payload.Content, payload.ReplyToId),
			Context.ConnectionAborted);

		if (result.IsFailure)
		{
			// success path is fanned-out by IChannelBroadcaster inside the
			// handler; the caller learns about it through ReceiveMessage like
			// every other subscriber, so the hub method itself only surfaces
			// failures
			await Clients.Caller.Error(result.Error.Code, result.Error.Message);
		}
	}
}
