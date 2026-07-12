using Chat.Application.Abstractions;
using Chat.Application.Contracts;
using Chat.Domain.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;

namespace Chat.Presentation.Hubs;

[Authorize]
public sealed class ChatHub(
	IGuildClient guildClient,
	IUserClient userClient,
	UserConnectionTracker connectionTracker,
	IEventBus eventBus,
	IClock clock,
	IMemoryCache cache) : Hub<IChatClient>
{
	private const long SendMessages = 1L << 0;
	private const long ReadMessages = 1L << 1;
	private const long Administrator = 1L << 8;

	// In seconds
	private const int TypingRateLimitDuration = 3;
	private const int TypingExpirationDelay = 8;

	public override async Task OnConnectedAsync()
	{
		var isFirstConnection = connectionTracker.TrackConnected(Context.GetUserId(), Context.ConnectionId, Context);
		if (isFirstConnection)
			await eventBus.PublishAsync(new UserOnline(Context.GetUserId()), CancellationToken.None);

		// subscribe this connection to the guild:{id} group of every guild the
		// user belongs to, so guild-scoped broadcasts (channel/member/role
		// changes, presence) reach them. a Guild outage must not reject the
		// connection: log-and-continue, the client still gets DMs + channel
		// broadcasts it explicitly joins.
		try
		{
			var guildIds = await guildClient.GetUserGuildIdsAsync(Context.GetUserId(), Context.ConnectionAborted);
			foreach (var guildId in guildIds)
				await Groups.AddToGroupAsync(Context.ConnectionId, $"guild:{guildId}", Context.ConnectionAborted);

			// only the first connection flips the user to online; extra tabs are
			// already covered. tell co-guild-members and friends live.
			if (isFirstConnection)
				await BroadcastPresenceAsync(Context.GetUserId(), "online", guildIds, Context.ConnectionAborted);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			// swallow: guild-group membership + presence are best-effort real-time sugar
		}

		await base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		if (connectionTracker.TrackDisconnected(Context.GetUserId(), Context.ConnectionId))
		{
			await eventBus.PublishAsync(new UserOffline(Context.GetUserId()), CancellationToken.None);

			// last connection closed: the user is now offline for everyone who can
			// see them. best-effort, must not throw out of the disconnect path.
			try
			{
				var guildIds = await guildClient.GetUserGuildIdsAsync(Context.GetUserId(), CancellationToken.None);
				await BroadcastPresenceAsync(Context.GetUserId(), "offline", guildIds, CancellationToken.None);
			}
			catch
			{
				// swallow: presence is best-effort
			}
		}

		await base.OnDisconnectedAsync(exception);
	}

	// fans a presence change out to everyone who may see the user: every guild
	// group they belong to (co-members) plus their friends (who may share no
	// guild). a friend who is also a co-member receives it twice; the client
	// handler is idempotent so that is harmless.
	private async Task BroadcastPresenceAsync(long userId, string status, IReadOnlyList<long> guildIds, CancellationToken ct)
	{
		var evt = new UserPresenceEvent(userId.ToString(), status);

		foreach (var guildId in guildIds)
			await Clients.Group($"guild:{guildId}").UserPresence(evt);

		var friendIds = await userClient.GetFriendIdsAsync(userId, ct);
		if (friendIds.Count > 0)
			await Clients.Users(friendIds.Select(id => id.ToString()).ToList()).UserPresence(evt);
	}

	public async Task JoinChannel(long channelId)
	{
		var membership = await guildClient.GetMembershipAsync(channelId, Context.GetUserId(), Context.ConnectionAborted);

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
		connectionTracker.TrackChannelJoined(Context.GetUserId(), Context.ConnectionId, channelId, membership.GuildId);
	}

	public async Task LeaveChannel(long channelId)
	{
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel:{channelId}");
		connectionTracker.TrackChannelLeft(Context.GetUserId(), Context.ConnectionId, channelId);
	}

	public async Task Typing(string scope, long id)
	{
		var now = clock.UtcNow;
		var cacheKey = $"typing:{Context.GetUserId()}:{scope}:{id}";

		if (cache.TryGetValue(cacheKey, out DateTimeOffset until) && until > now)
			return;

		var err = scope switch
		{
			"channel" => await ValidateChannelTypingAsync(id),
			"dm"      => await ValidateDmTypingAsync(id),
			_         => new Failure("Typing.InvalidScope", "Invalid scope provided")
		};

		if (err is not null)
		{
			await Clients.Caller.Error(err.Code, err.Message);
			return;
		}

 		// Store expiry in the value so the check above uses IClock (testable); cache TTL is only for eviction.
		var expiry = now.AddSeconds(TypingRateLimitDuration);
		cache.Set(cacheKey, expiry, TimeSpan.FromSeconds(TypingRateLimitDuration));

		var recipient = (scope == "channel") ? Clients.OthersInGroup($"channel:{id}") : Clients.User(id.ToString());
		await recipient.TypingStarted(Context.GetUserId().ToString(), scope, id.ToString(), now.AddSeconds(TypingExpirationDelay));
	}

	private async Task<Failure?> ValidateChannelTypingAsync(long channelId)
	{
		var membership = await guildClient.GetMembershipAsync(channelId, Context.GetUserId(), Context.ConnectionAborted);
		if (membership is null)
			return MessageFailures.ChannelNotFound;

		if (!membership.IsMember)
			return MessageFailures.NotAMember;
		if ((membership.Permissions & (SendMessages | Administrator)) == 0)
			return MessageFailures.MissingSendPermission;
		return null;
	}

	private async Task<Failure?> ValidateDmTypingAsync(long partnerId)
	{
		var relationship = await userClient.GetUsersRelationship(Context.GetUserId(), partnerId, Context.ConnectionAborted);
		if (relationship is null)
			return MessageFailures.UserNotFound;

		return relationship.Status switch
		{
			"accepted"                           => null,
			"blocked_by_me" or "blocked_by_them" => MessageFailures.RecipientBlocked,
			_                            => MessageFailures.RecipientNotFriend
		};
	}
}
