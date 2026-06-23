using Chat.Application.Abstractions;
using Chat.Application.Contracts;
using Chat.Application.Features.Messages.Common;
using Chat.FunctionalTests.Infrastructure;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

namespace Chat.FunctionalTests.Features;

// ─────────────────────────────────────────────────────────────────────────────
// T1.01 – T1.09 · Hub connection & user presence events
// ─────────────────────────────────────────────────────────────────────────────
public sealed class HubConnectionTests(ChatApiFactory factory)
	: IClassFixture<ChatApiFactory>, IAsyncLifetime
{
	// Unique IDs per test — UserConnectionTracker is a singleton shared across
	// all tests in this class, so each test must use a distinct userId to avoid
	// cross-contamination.
	private const long UserValidToken        = 1_001;
	private const long UserExpiredToken      = 1_002;
	private const long UserOnlineEvent       = 1_003;
	private const long UserOfflineEvent      = 1_004;
	private const long UserMultiConnOnline   = 1_005;
	private const long UserMultiConnNoOff    = 1_006;
	private const long UserMultiConnLastOff  = 1_007;

	public Task InitializeAsync() { factory.EventBus.Reset(); return Task.CompletedTask; }
	public Task DisposeAsync() => Task.CompletedTask;

	// T1.01 — connection without token rejected
	[Fact]
	public async Task Connect_WithoutToken_IsRejected()
	{
		var conn = HubConnectionHelper.Build(factory, token: null);
		await Assert.ThrowsAnyAsync<Exception>(() => conn.StartAsync());
		Assert.NotEqual(HubConnectionState.Connected, conn.State);
		await conn.DisposeAsync();
	}

	// T1.02 — connection with valid token succeeds
	[Fact]
	public async Task Connect_WithValidToken_Succeeds()
	{
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserValidToken);
		await using var conn = HubConnectionHelper.Build(factory, token);
		await conn.StartAsync();
		Assert.Equal(HubConnectionState.Connected, conn.State);
	}

	// T1.03 — connection with expired token rejected

	[Fact]
	public async Task Connect_WithExpiredToken_IsRejected()
	{
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserExpiredToken,
			expiresAt: DateTimeOffset.UtcNow.AddHours(-1));
		var conn = HubConnectionHelper.Build(factory, token);
		await Assert.ThrowsAnyAsync<Exception>(() => conn.StartAsync());
		await conn.DisposeAsync();
	}

	// T1.04 — connection publish user.online
	[Fact]
	public async Task Connect_PublishesUserOnlineEvent()
	{
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserOnlineEvent);
		await using var conn = HubConnectionHelper.Build(factory, token);

		await conn.StartAsync();

		Assert.Contains(factory.EventBus.PublishedOf<UserOnline>(), e => e.UserId == UserOnlineEvent);
	}

	// T1.05 — disconnection publish user.offline
	[Fact]
	public async Task Disconnect_PublishesUserOfflineEvent()
	{
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserOfflineEvent);
		var conn = HubConnectionHelper.Build(factory, token);
		await conn.StartAsync();
		factory.EventBus.Reset();

		await conn.DisposeAsync();
		await Task.Delay(200);

		Assert.Contains(factory.EventBus.PublishedOf<UserOffline>(), e => e.UserId == UserOfflineEvent);
	}

	// T1.07 — two connections for the same user → user.online published once
	[Fact]
	public async Task TwoConnections_SameUser_UserOnlinePublishedOnce()
	{
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserMultiConnOnline);
		await using var conn1 = HubConnectionHelper.Build(factory, token);
		await using var conn2 = HubConnectionHelper.Build(factory, token);

		await conn1.StartAsync();
		await conn2.StartAsync();

		var events = factory.EventBus.PublishedOf<UserOnline>().Where(e => e.UserId == UserMultiConnOnline).ToList();
		Assert.Single(events);
	}

	// T1.08 — one of thems disconnect (but the other stay) → user.offline not published
	[Fact]
	public async Task OneOfTwoDisconnects_UserOfflineNotPublished()
	{
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserMultiConnNoOff);
		await using var conn1 = HubConnectionHelper.Build(factory, token);
		var conn2 = HubConnectionHelper.Build(factory, token);
		await conn1.StartAsync();
		await conn2.StartAsync();
		factory.EventBus.Reset();

		await conn2.DisposeAsync();
		await Task.Delay(200);

		Assert.DoesNotContain(factory.EventBus.PublishedOf<UserOffline>(), e => e.UserId == UserMultiConnNoOff);
	}

	// T1.09 — last connection leave → user.offline published
	[Fact]
	public async Task LastConnectionDisconnects_UserOfflinePublished()
	{
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserMultiConnLastOff);
		var conn1 = HubConnectionHelper.Build(factory, token);
		var conn2 = HubConnectionHelper.Build(factory, token);
		await conn1.StartAsync();
		await conn2.StartAsync();
		factory.EventBus.Reset();

		await conn1.DisposeAsync();
		await Task.Delay(150);
		Assert.DoesNotContain(factory.EventBus.PublishedOf<UserOffline>(), e => e.UserId == UserMultiConnLastOff);

		await conn2.DisposeAsync();
		await Task.Delay(200);
		Assert.Contains(factory.EventBus.PublishedOf<UserOffline>(), e => e.UserId == UserMultiConnLastOff);
	}
}

// ─────────────────────────────────────────────────────────────────────────────
// T1.10 – T1.17 · JoinChannel
// ─────────────────────────────────────────────────────────────────────────────

public sealed class JoinChannelTests(ChatApiFactory factory)
	: IClassFixture<ChatApiFactory>, IAsyncLifetime
{
	private const long ReadMessages  = 1L << 1;
	private const long Administrator = 1L << 8;
	private const long GuildId       = 999;
	private const long ChannelId     = 100;

	private const long UserMember        = 2_001;
	private const long UserNotMember     = 2_002;
	private const long UserNoPerm        = 2_003;
	private const long UserAdmin         = 2_004;
	private const long UserReceive       = 2_005;
	private const long UserNoReceive     = 2_006;
	private const long UserIdempotent    = 2_007;

	public Task InitializeAsync() { factory.EventBus.Reset(); return Task.CompletedTask; }
	public Task DisposeAsync() => Task.CompletedTask;

	// T1.10 — valid member + READ_MESSAGES → succeed
	[Fact]
	public async Task JoinChannel_ValidMemberWithReadPerm_NoError()
	{
		factory.WithMembershipFor(ChannelId, UserMember, GuildId, ReadMessages);
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserMember);
		await using var conn = HubConnectionHelper.Build(factory, token);

		var errorReceived = false;
		conn.On<string, string>("Error", (_, _) => errorReceived = true);

		await conn.StartAsync();
		await conn.InvokeAsync("JoinChannel", ChannelId);
		await Task.Delay(200);

		Assert.False(errorReceived);
	}

	// T1.11 — membership null (unknow channel) → Error("Channel.NotFound")
	[Fact]
	public async Task JoinChannel_NullMembership_SendsChannelNotFoundError()
	{
		factory.WithoutMembershipFor(ChannelId, UserNotMember);
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserNotMember);
		await using var conn = HubConnectionHelper.Build(factory, token);

		var errorTcs = new TaskCompletionSource<string>();
		conn.On<string, string>("Error", (code, _) => errorTcs.TrySetResult(code));

		await conn.StartAsync();
		await conn.InvokeAsync("JoinChannel", ChannelId);

		var code = await errorTcs.Task.WaitAsync(TimeSpan.FromSeconds(3));
		Assert.Equal("Channel.NotFound", code);
	}

	// T1.12 — not a member → Error("Channel.NotFound")
	[Fact]
	public async Task JoinChannel_NotMember_SendsChannelNotFoundError()
	{
		factory.GuildClient.Setup(ChannelId, UserNotMember + 1,
			new ChannelMembership(IsMember: false, GuildId: GuildId, Permissions: ReadMessages));
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserNotMember + 1);
		await using var conn = HubConnectionHelper.Build(factory, token);

		var errorTcs = new TaskCompletionSource<string>();
		conn.On<string, string>("Error", (code, _) => errorTcs.TrySetResult(code));

		await conn.StartAsync();
		await conn.InvokeAsync("JoinChannel", ChannelId);

		var code = await errorTcs.Task.WaitAsync(TimeSpan.FromSeconds(3));
		Assert.Equal("Channel.NotFound", code);
	}

	// T1.13 — valid member but no perms → Error("Channel.MissingReadPermission")
	[Fact]
	public async Task JoinChannel_MemberWithNoPermissions_SendsMissingReadPermError()
	{
		factory.GuildClient.Setup(ChannelId, UserNoPerm,
			new ChannelMembership(IsMember: true, GuildId: GuildId, Permissions: 0));
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserNoPerm);
		await using var conn = HubConnectionHelper.Build(factory, token);

		var errorTcs = new TaskCompletionSource<string>();
		conn.On<string, string>("Error", (code, _) => errorTcs.TrySetResult(code));

		await conn.StartAsync();
		await conn.InvokeAsync("JoinChannel", ChannelId);

		var code = await errorTcs.Task.WaitAsync(TimeSpan.FromSeconds(3));
		Assert.Equal("Channel.MissingReadPermission", code);
	}

	// T1.14 — ADMINISTRATOR only (without READ_MESSAGES) → succeed
	[Fact]
	public async Task JoinChannel_AdministratorOnly_Succeeds()
	{
		factory.GuildClient.Setup(ChannelId, UserAdmin,
			new ChannelMembership(IsMember: true, GuildId: GuildId, Permissions: Administrator));
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserAdmin);
		await using var conn = HubConnectionHelper.Build(factory, token);

		var errorReceived = false;
		conn.On<string, string>("Error", (_, _) => errorReceived = true);

		await conn.StartAsync();
		await conn.InvokeAsync("JoinChannel", ChannelId);
		await Task.Delay(200);

		Assert.False(errorReceived);
	}

	// T1.15 — after JoinChannel, when messages is sent → get ReceiveMessage
	[Fact]
	public async Task JoinChannel_ThenBroadcast_ClientReceivesMessage()
	{
		factory.WithMembershipFor(ChannelId, UserReceive, GuildId, ReadMessages);
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserReceive);
		await using var conn = HubConnectionHelper.Build(factory, token);

		var messageTcs = new TaskCompletionSource<MessageResponse>();
		conn.On<MessageResponse>("ReceiveMessage", msg => messageTcs.TrySetResult(msg));

		await conn.StartAsync();
		await conn.InvokeAsync("JoinChannel", ChannelId);

		var testMessage = new MessageResponse(
			Id: "42", ChannelId: ChannelId.ToString(), AuthorId: "1",
			Content: "hello", ReplyToId: null, EditedAt: null,
			CreatedAt: DateTimeOffset.UtcNow, Attachments: [], Reactions: [], Nonce: null);

		await factory.SimulateChannelMessageAsync(ChannelId, testMessage);

		var received = await messageTcs.Task.WaitAsync(TimeSpan.FromSeconds(3));
		Assert.Equal("42", received.Id);
		Assert.Equal("hello", received.Content);
	}

	// T1.16 — without JoinChannel, when messages is sent → doesn't get ReceiveMessage
	[Fact]
	public async Task WithoutJoinChannel_Broadcast_ClientDoesNotReceiveMessage()
	{
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserNoReceive);
		await using var conn = HubConnectionHelper.Build(factory, token);

		var received = false;
		conn.On<MessageResponse>("ReceiveMessage", _ => received = true);

		await conn.StartAsync();

		var testMessage = new MessageResponse(
			Id: "99", ChannelId: ChannelId.ToString(), AuthorId: "1",
			Content: "ghost", ReplyToId: null, EditedAt: null,
			CreatedAt: DateTimeOffset.UtcNow, Attachments: [], Reactions: [], Nonce: null);

		await factory.SimulateChannelMessageAsync(ChannelId, testMessage);
		await Task.Delay(300);

		Assert.False(received);
	}

	// T1.17 — JoinChannel two time the same channel → idempotent, the second try is a no-op
	[Fact]
	public async Task JoinChannel_Twice_IsIdempotent()
	{
		factory.WithMembershipFor(ChannelId, UserIdempotent, GuildId, ReadMessages);
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserIdempotent);
		await using var conn = HubConnectionHelper.Build(factory, token);

		var errorReceived = false;
		conn.On<string, string>("Error", (_, _) => errorReceived = true);

		await conn.StartAsync();
		await conn.InvokeAsync("JoinChannel", ChannelId);
		await conn.InvokeAsync("JoinChannel", ChannelId);
		await Task.Delay(200);

		Assert.False(errorReceived);
	}
}

// ─────────────────────────────────────────────────────────────────────────────
// T1.18 – T1.20 · LeaveChannel
// ─────────────────────────────────────────────────────────────────────────────

public sealed class LeaveChannelTests(ChatApiFactory factory)
	: IClassFixture<ChatApiFactory>, IAsyncLifetime
{
	private const long ReadMessages = 1L << 1;
	private const long GuildId      = 999;
	private const long ChannelId    = 200;

	private const long UserLeaveAfterJoin  = 3_001;
	private const long UserLeaveNoJoin     = 3_002;
	private const long UserLeaveWrongId    = 3_003;

	public Task InitializeAsync() { factory.EventBus.Reset(); return Task.CompletedTask; }
	public Task DisposeAsync() => Task.CompletedTask;

	// T1.18 — LeaveChannel after JoinChannel → doesn't get ReceiveMessage anymore
	[Fact]
	public async Task LeaveChannel_AfterJoin_StopsReceivingMessages()
	{
		factory.WithMembershipFor(ChannelId, UserLeaveAfterJoin, GuildId, ReadMessages);
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserLeaveAfterJoin);
		await using var conn = HubConnectionHelper.Build(factory, token);

		var received = false;
		conn.On<MessageResponse>("ReceiveMessage", _ => received = true);

		await conn.StartAsync();
		await conn.InvokeAsync("JoinChannel", ChannelId);
		await conn.InvokeAsync("LeaveChannel", ChannelId);

		var testMessage = new MessageResponse(
			Id: "1", ChannelId: ChannelId.ToString(), AuthorId: "1",
			Content: "after leave", ReplyToId: null, EditedAt: null,
			CreatedAt: DateTimeOffset.UtcNow, Attachments: [], Reactions: [], Nonce: null);

		await factory.SimulateChannelMessageAsync(ChannelId, testMessage);
		await Task.Delay(300);

		Assert.False(received);
	}

	// T1.19 — LeaveChannel without JoinChannel → no op, a connection can always leave a channel
	[Fact]
	public async Task LeaveChannel_WithoutPriorJoin_NoError()
	{
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserLeaveNoJoin);
		await using var conn = HubConnectionHelper.Build(factory, token);

		var errorReceived = false;
		conn.On<string, string>("Error", (_, _) => errorReceived = true);

		await conn.StartAsync();
		await conn.InvokeAsync("LeaveChannel", ChannelId);
		await Task.Delay(200);

		Assert.False(errorReceived);
		Assert.Equal(HubConnectionState.Connected, conn.State);
	}

	// T1.20 — LeaveChannel with unknow channelId → no op, for the same reason as above
	[Fact]
	public async Task LeaveChannel_NonExistentChannel_NoError()
	{
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserLeaveWrongId);
		await using var conn = HubConnectionHelper.Build(factory, token);

		var errorReceived = false;
		conn.On<string, string>("Error", (_, _) => errorReceived = true);

		await conn.StartAsync();
		await conn.InvokeAsync("LeaveChannel", 999_999_999L);
		await Task.Delay(200);

		Assert.False(errorReceived);
		Assert.Equal(HubConnectionState.Connected, conn.State);
	}
}

// ─────────────────────────────────────────────────────────────────────────────
// T1.21 – T1.24 · Guild events (GuildJoined / GuildLeft)
// ─────────────────────────────────────────────────────────────────────────────

public sealed class GuildEventTests(ChatApiFactory factory)
	: IClassFixture<ChatApiFactory>, IAsyncLifetime
{
	private const long GuildId     = 999;
	private const string GuildName = "TestGuild";

	private const long UserGuildJoined       = 4_001;
	private const long UserGuildLeft         = 4_002;
	private const long UserGuildJoinedOther  = 4_003;
	private const long UserGuildLeftOther    = 4_004;

	public Task InitializeAsync() { factory.EventBus.Reset(); return Task.CompletedTask; }
	public Task DisposeAsync() => Task.CompletedTask;

	// T1.21 — SimulateGuildJoined → client get GuildJoined(guildId, guildName)
	[Fact]
	public async Task GuildJoined_ConnectedUser_ReceivesGuildJoinedInvocation()
	{
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserGuildJoined);
		await using var conn = HubConnectionHelper.Build(factory, token);

		var tcs = new TaskCompletionSource<(string GuildId, string GuildName)>();
		conn.On<string, string>("GuildJoined", (gId, gName) => tcs.TrySetResult((gId, gName)));

		await conn.StartAsync();
		await factory.SimulateGuildJoinedAsync(UserGuildJoined, GuildId, GuildName);

		var result = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(3));
		Assert.Equal(GuildId.ToString(), result.GuildId);
		Assert.Equal(GuildName, result.GuildName);
	}

	// T1.22 — SimulateGuildJoined for a unconnected user → no crash
	[Fact]
	public async Task GuildJoined_DisconnectedUser_NoException()
	{
		var exception = await Record.ExceptionAsync(
			() => factory.SimulateGuildJoinedAsync(userId: 99_999, GuildId, GuildName));

		Assert.Null(exception);
	}

	// T1.23 — SimulateGuildLeft → client get GuildLeft(guildId)
	[Fact]
	public async Task GuildLeft_ConnectedUser_ReceivesGuildLeftInvocation()
	{
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserGuildLeft);
		await using var conn = HubConnectionHelper.Build(factory, token);

		var tcs = new TaskCompletionSource<string>();
		conn.On<string>("GuildLeft", gId => tcs.TrySetResult(gId));

		await conn.StartAsync();
		await factory.SimulateGuildLeftAsync(UserGuildLeft, GuildId);

		var receivedGuildId = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(3));
		Assert.Equal(GuildId.ToString(), receivedGuildId);
	}

	// T1.24 — GuildJoined is broadcasted to only one user, the one who join the guild
	[Fact]
	public async Task GuildJoined_BroadcastedToTargetOnly_OtherUserDoesNotReceive()
	{
		var tokenA = TestTokens.Issue(ChatApiFactory.JwtSecret, UserGuildJoinedOther);
		var tokenB = TestTokens.Issue(ChatApiFactory.JwtSecret, UserGuildLeftOther);

		await using var connA = HubConnectionHelper.Build(factory, tokenA);
		await using var connB = HubConnectionHelper.Build(factory, tokenB);

		var bReceived = false;
		connB.On<string, string>("GuildJoined", (_, _) => bReceived = true);

		await connA.StartAsync();
		await connB.StartAsync();

		await factory.SimulateGuildJoinedAsync(UserGuildJoinedOther, GuildId, GuildName);
		await Task.Delay(300);

		Assert.False(bReceived);
	}
}

// ─────────────────────────────────────────────────────────────────────────────
// T1.25 – T1.28 · Server-side eviction (guild.member_left / kick / ban)
// ─────────────────────────────────────────────────────────────────────────────

public sealed class ChannelEvictionTests(ChatApiFactory factory)
	: IClassFixture<ChatApiFactory>, IAsyncLifetime
{
	private const long ReadMessages = 1L << 1;
	private const long GuildId      = 777;
	private const long OtherGuildId = 778;
	private const long ChannelId    = 400;
	private const long OtherChannel = 401;

	private const long UserEvicted       = 5_001;
	private const long UserOtherGuild    = 5_002;
	private const long UserRejoinAfter   = 5_003;
	private const long UserChannelEvict  = 5_004;

	public Task InitializeAsync() { factory.EventBus.Reset(); return Task.CompletedTask; }
	public Task DisposeAsync() => Task.CompletedTask;

	// T1.25 - after guild eviction, a joined member stops receiving channel messages
	[Fact]
	public async Task GuildEviction_AfterJoin_StopsReceivingMessages()
	{
		factory.WithMembershipFor(ChannelId, UserEvicted, GuildId, ReadMessages);
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserEvicted);
		await using var conn = HubConnectionHelper.Build(factory, token);

		var received = false;
		conn.On<MessageResponse>("ReceiveMessage", _ => received = true);

		await conn.StartAsync();
		await conn.InvokeAsync("JoinChannel", ChannelId);

		await factory.SimulateGuildEvictionAsync(UserEvicted, GuildId);

		await factory.SimulateChannelMessageAsync(ChannelId, Message(ChannelId, "after eviction"));
		await Task.Delay(300);

		Assert.False(received);
	}

	// T1.26 - eviction only targets channels of the named guild
	[Fact]
	public async Task GuildEviction_LeavesOtherGuildChannelsSubscribed()
	{
		factory.GuildClient.Setup(ChannelId, UserOtherGuild,
			new ChannelMembership(IsMember: true, GuildId: GuildId, Permissions: ReadMessages));
		factory.GuildClient.Setup(OtherChannel, UserOtherGuild,
			new ChannelMembership(IsMember: true, GuildId: OtherGuildId, Permissions: ReadMessages));
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserOtherGuild);
		await using var conn = HubConnectionHelper.Build(factory, token);

		var otherReceived = new TaskCompletionSource<MessageResponse>();
		conn.On<MessageResponse>("ReceiveMessage", msg =>
		{
			if (msg.ChannelId == OtherChannel.ToString())
				otherReceived.TrySetResult(msg);
		});

		await conn.StartAsync();
		await conn.InvokeAsync("JoinChannel", ChannelId);
		await conn.InvokeAsync("JoinChannel", OtherChannel);

		await factory.SimulateGuildEvictionAsync(UserOtherGuild, GuildId);

		await factory.SimulateChannelMessageAsync(OtherChannel, Message(OtherChannel, "still here"));

		var received = await otherReceived.Task.WaitAsync(TimeSpan.FromSeconds(3));
		Assert.Equal("still here", received.Content);
	}

	// T1.27 - an evicted user can re-join (e.g. re-invited) and receive again
	[Fact]
	public async Task GuildEviction_ThenRejoin_ReceivesMessagesAgain()
	{
		factory.WithMembershipFor(ChannelId, UserRejoinAfter, GuildId, ReadMessages);
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserRejoinAfter);
		await using var conn = HubConnectionHelper.Build(factory, token);

		var messageTcs = new TaskCompletionSource<MessageResponse>();
		conn.On<MessageResponse>("ReceiveMessage", msg => messageTcs.TrySetResult(msg));

		await conn.StartAsync();
		await conn.InvokeAsync("JoinChannel", ChannelId);
		await factory.SimulateGuildEvictionAsync(UserRejoinAfter, GuildId);

		await conn.InvokeAsync("JoinChannel", ChannelId);
		await factory.SimulateChannelMessageAsync(ChannelId, Message(ChannelId, "welcome back"));

		var received = await messageTcs.Task.WaitAsync(TimeSpan.FromSeconds(3));
		Assert.Equal("welcome back", received.Content);
	}

	// T1.28 - channel-scoped eviction drops one channel but leaves the rest
	[Fact]
	public async Task ChannelEviction_DropsTargetChannelOnly()
	{
		factory.GuildClient.Setup(ChannelId, UserChannelEvict,
			new ChannelMembership(IsMember: true, GuildId: GuildId, Permissions: ReadMessages));
		factory.GuildClient.Setup(OtherChannel, UserChannelEvict,
			new ChannelMembership(IsMember: true, GuildId: GuildId, Permissions: ReadMessages));
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserChannelEvict);
		await using var conn = HubConnectionHelper.Build(factory, token);

		var evictedReceived = false;
		var survivorReceived = new TaskCompletionSource<MessageResponse>();
		conn.On<MessageResponse>("ReceiveMessage", msg =>
		{
			if (msg.ChannelId == ChannelId.ToString())
				evictedReceived = true;
			else if (msg.ChannelId == OtherChannel.ToString())
				survivorReceived.TrySetResult(msg);
		});

		await conn.StartAsync();
		await conn.InvokeAsync("JoinChannel", ChannelId);
		await conn.InvokeAsync("JoinChannel", OtherChannel);

		await factory.SimulateChannelEvictionAsync(UserChannelEvict, ChannelId);

		await factory.SimulateChannelMessageAsync(ChannelId, Message(ChannelId, "gone"));
		await factory.SimulateChannelMessageAsync(OtherChannel, Message(OtherChannel, "kept"));

		var received = await survivorReceived.Task.WaitAsync(TimeSpan.FromSeconds(3));
		Assert.Equal("kept", received.Content);
		Assert.False(evictedReceived);
	}

	private static MessageResponse Message(long channelId, string content) =>
		new(Id: "1", ChannelId: channelId.ToString(), AuthorId: "1",
			Content: content, ReplyToId: null, EditedAt: null,
			CreatedAt: DateTimeOffset.UtcNow, Attachments: [], Reactions: [], Nonce: null);
}
