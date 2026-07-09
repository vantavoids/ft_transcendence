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
		if (connectionTracker.TrackConnected(Context.GetUserId(), Context.ConnectionId, Context))
			await eventBus.PublishAsync(new UserOnline(Context.GetUserId()), CancellationToken.None);
		await base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		if (connectionTracker.TrackDisconnected(Context.GetUserId(), Context.ConnectionId))
			await eventBus.PublishAsync(new UserOffline(Context.GetUserId()), CancellationToken.None);
		await base.OnDisconnectedAsync(exception);
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
