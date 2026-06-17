using System.Collections.Concurrent;
using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Authentication;
using Chat.Application.Contracts;
using Chat.Domain.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Chat.Presentation.Hubs;

[Authorize]
public sealed class ChatHub(
	IGuildClient guildClient,
	IUserClient userClient,
	ICurrentUser currentUser,
	UserConnectionTracker connectionTracker,
	IEventBus eventBus,
	IClock clock) : Hub<IChatClient>
{
	private const long SendMessages = 1L << 0;
	private const long ReadMessages = 1L << 1;
	private const long Administrator = 1L << 8;

	// ? In seconds
	private const int TypingRateLimitDuration = 3;
	private const int TypingExpirationDelay = 8;

	public override async Task OnConnectedAsync()
	{
		if (connectionTracker.TrackConnected(currentUser.UserId))
			await eventBus.PublishAsync(new UserOnline(currentUser.UserId), CancellationToken.None);
		await base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		if (connectionTracker.TrackDisconnected(currentUser.UserId))
			await eventBus.PublishAsync(new UserOffline(currentUser.UserId), CancellationToken.None);
		await base.OnDisconnectedAsync(exception);
	}

	private static readonly ConcurrentDictionary<(long, string, long), DateTimeOffset> _ratelimit = new();

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

	public async Task Typing(string scope, long id)
	{
		var now = clock.UtcNow;
		var key = (currentUser.UserId, scope, id);

		if (_ratelimit.TryGetValue(key, out var until) && until > now)
			return;
		_ratelimit[key] = now.AddSeconds(TypingRateLimitDuration);

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

		var recipient = (scope == "channel") ? Clients.OthersInGroup($"channel:{id}") : Clients.User(id.ToString());
		await recipient.TypingStarted(currentUser.UserId.ToString(), scope, id.ToString(), now.AddSeconds(TypingExpirationDelay));
	}

	private async Task<Failure?> ValidateChannelTypingAsync(long channelId)
	{
		var membership = await guildClient.GetMembershipAsync(channelId, currentUser.UserId, Context.ConnectionAborted);
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
		var relationship = await userClient.GetUsersRelationship(currentUser.UserId, partnerId, Context.ConnectionAborted);
		if (relationship is null)
			return MessageFailures.UserNotFound;

		return relationship.Status switch
		{
			"accepted"                         => null,
			"blocked_by_me" or "blocked_by_them" => MessageFailures.RecipientBlocked,
			_                                  => MessageFailures.RecipientNotFriend
		};
	}
}
