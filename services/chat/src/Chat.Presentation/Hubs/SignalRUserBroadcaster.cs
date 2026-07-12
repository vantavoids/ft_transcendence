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

	public Task BroadcastGuildDeletedAsync(long guildId, CancellationToken ct) =>
		hub.Clients.Group($"guild:{guildId}").GuildDeleted(guildId.ToString());

	public Task BroadcastGuildUpdatedAsync(long guildId, string name, string? iconUrl, CancellationToken ct) =>
		hub.Clients.Group($"guild:{guildId}").GuildUpdated(new GuildUpdatedEvent(guildId.ToString(), name, iconUrl));

	public async Task AddUserToGuildGroupAsync(long userId, long guildId, CancellationToken ct)
	{
		foreach (var connectionId in tracker.ConnectionIds(userId))
			await hub.Groups.AddToGroupAsync(connectionId, $"guild:{guildId}", ct);
	}

	public async Task RemoveUserFromGuildGroupAsync(long userId, long guildId, CancellationToken ct)
	{
		foreach (var connectionId in tracker.ConnectionIds(userId))
			await hub.Groups.RemoveFromGroupAsync(connectionId, $"guild:{guildId}", ct);
	}

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

	public Task BroadcastChannelCreatedAsync(IReadOnlyList<long> userIds, GuildChannelDto channel, CancellationToken ct) =>
		hub.Clients.Users(userIds.Select(id => id.ToString()).ToList()).ChannelCreated(channel);

	public Task BroadcastChannelUpdatedAsync(IReadOnlyList<long> userIds, GuildChannelDto channel, CancellationToken ct) =>
		hub.Clients.Users(userIds.Select(id => id.ToString()).ToList()).ChannelUpdated(channel);

	public Task BroadcastChannelDeletedAsync(IReadOnlyList<long> userIds, long guildId, long channelId, CancellationToken ct) =>
		hub.Clients.Users(userIds.Select(id => id.ToString()).ToList())
			.ChannelDeleted(new ChannelDeletedEvent(guildId.ToString(), channelId.ToString()));

	public Task BroadcastMemberJoinedAsync(long guildId, long userId, CancellationToken ct) =>
		hub.Clients.Group($"guild:{guildId}").MemberJoined(new GuildMemberEvent(guildId.ToString(), userId.ToString()));

	public Task BroadcastMemberLeftAsync(long guildId, long userId, CancellationToken ct) =>
		hub.Clients.Group($"guild:{guildId}").MemberLeft(new GuildMemberEvent(guildId.ToString(), userId.ToString()));

	public Task BroadcastCategoryCreatedAsync(long guildId, GuildCategoryDto category, CancellationToken ct) =>
		hub.Clients.Group($"guild:{guildId}").CategoryCreated(category);

	public Task BroadcastCategoryUpdatedAsync(long guildId, GuildCategoryDto category, CancellationToken ct) =>
		hub.Clients.Group($"guild:{guildId}").CategoryUpdated(category);

	public Task BroadcastCategoryDeletedAsync(long guildId, long categoryId, CancellationToken ct) =>
		hub.Clients.Group($"guild:{guildId}").CategoryDeleted(new CategoryDeletedEvent(guildId.ToString(), categoryId.ToString()));

	public Task BroadcastRolesChangedAsync(long guildId, CancellationToken ct) =>
		hub.Clients.Group($"guild:{guildId}").RolesChanged(guildId.ToString());

	public Task BroadcastMemberUpdatedAsync(long guildId, long userId, CancellationToken ct) =>
		hub.Clients.Group($"guild:{guildId}").MemberUpdated(new GuildMemberEvent(guildId.ToString(), userId.ToString()));

	public Task BroadcastChannelAccessGrantedAsync(long userId, long guildId, long channelId, CancellationToken ct) =>
		hub.Clients.User(userId.ToString()).ChannelAccessGranted(new ChannelAccessGrantedEvent(guildId.ToString(), channelId.ToString()));

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
