using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Authentication;
using Chat.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Chat.Presentation.Hubs;

[Authorize]
public sealed class ChatHub(IGuildClient guildClient, ICurrentUser currentUser, IEventBus eventBus) : Hub<IChatClient>
{
	private const long ReadMessages = 1L << 1;
	private const long Administrator = 1L << 8;

	public override async Task OnConnectedAsync()
	{
		await eventBus.PublishAsync(new UserOnline(currentUser.UserId), CancellationToken.None);
		await base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		await eventBus.PublishAsync(new UserOffline(currentUser.UserId), CancellationToken.None);
		await base.OnDisconnectedAsync(exception);
	}

	public async Task JoinChannel(long channelId)
	{
		var membership = await guildClient.GetMembershipAsync(channelId, currentUser.UserId, Context.ConnectionAborted);

		if (membership is null || !membership.IsMember)
		{
			await Clients.Caller.Error("Channel.NotFound", "Channel not found.");
			return;
		}

		if ((membership.Permissions & (ReadMessages | Administrator)) == 0)
		{
			await Clients.Caller.Error("Channel.MissingReadPermission", "Caller lacks READ_MESSAGES permission on this channel.");
			return;
		}

		await Groups.AddToGroupAsync(Context.ConnectionId, $"channel:{channelId}", Context.ConnectionAborted);
	}

	public Task LeaveChannel(long channelId) =>
		Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel:{channelId}");
}
