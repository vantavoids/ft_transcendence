using Chat.Application.Abstractions;
using Chat.Application.Features.Channels.Common;
using Chat.Application.Features.DirectMessages.Common;
using Microsoft.AspNetCore.SignalR;

namespace Chat.Presentation.Hubs;

/// <summary>
/// routes per-user payloads to every connection the user has open using
/// SignalR's built-in user routing. resolution goes through
/// <c>DefaultUserIdProvider</c>, which reads <c>ClaimTypes.NameIdentifier</c>
/// off the JWT-authenticated identity (the user's snowflake string, since the
/// JwtBearer middleware auto-maps the <c>sub</c> claim to it)
/// </summary>
internal sealed class SignalRUserBroadcaster(
	IHubContext<ChatHub, IChatClient> hub,
	UserConnectionTracker tracker)
	: IUserBroadcaster
{
	public Task BroadcastGuildJoinedAsync(long userId, long guildId, string guildName, CancellationToken ct) =>
		hub.Clients.User(userId.ToString()).GuildJoined(guildId.ToString(), guildName);

	public Task BroadcastGuildLeftAsync(long userId, long guildId, CancellationToken ct) =>
		hub.Clients.User(userId.ToString()).GuildLeft(guildId.ToString());

	public Task<int> DisconnectUserAsync(long userId, CancellationToken ct)
	{
		var contexts = tracker.UserContexts(userId);
		foreach (var context in contexts)
			context.Abort();

		return Task.FromResult(contexts.Count);
	}

	public Task BroadcastChannelReadStateUpdatedAsync(long userId, ChannelReadStateResponse response, CancellationToken ct) =>
		hub.Clients.User(userId.ToString()).ReadStateUpdated(response);

	public Task BroadcastDmReadStateUpdatedAsync(long userId, DmReadStateResponse response, CancellationToken ct) =>
		hub.Clients.User(userId.ToString()).DmReadStateUpdated(response);

	public async Task<int> EvictFromGuildChannelsAsync(long userId, long guildId, CancellationToken ct)
	{
		var subscriptions = tracker.ConnectionsInGuild(userId, guildId);
		foreach (var (connectionId, channelId) in subscriptions)
		{
			await hub.Groups.RemoveFromGroupAsync(connectionId, $"channel:{channelId}", ct);
			tracker.TrackChannelLeft(userId, connectionId, channelId);
		}
		return subscriptions.Count;
	}

	public async Task<int> EvictFromChannelAsync(long userId, long channelId, CancellationToken ct)
	{
		var connections = tracker.ConnectionsInChannel(userId, channelId);
		foreach (var connectionId in connections)
		{
			await hub.Groups.RemoveFromGroupAsync(connectionId, $"channel:{channelId}", ct);
			tracker.TrackChannelLeft(userId, connectionId, channelId);
		}
		return connections.Count;
	}
}
