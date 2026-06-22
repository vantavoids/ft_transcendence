using Chat.Application.Abstractions;
using Chat.FunctionalTests.Infrastructure;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

namespace Chat.FunctionalTests.Features;

// ─────────────────────────────────────────────────────────────────────────────
// T4.01 – T4.10 · Typing — channel scope
//
// _ratelimit is a static ConcurrentDictionary inside ChatHub that persists for
// the entire process lifetime. every test uses unique (userId, channelId) pairs
// so no two tests can share a rate-limit entry.
// ─────────────────────────────────────────────────────────────────────────────
public sealed class ChannelTypingTests(ChatApiFactory factory)
	: IClassFixture<ChatApiFactory>, IAsyncLifetime
{
	private const long SendMessages  = 1L << 0;
	private const long ReadMessages  = 1L << 1;
	private const long Administrator = 1L << 8;

	// unique (userId, channelId) per test — static rate-limit isolation
	private const long UserHappyPath       = 7_001;
	private const long SubHappyPath        = 7_002;
	private const long ChHappyPath         = 101_001;

	private const long UserAdminOnly       = 7_003;
	private const long SubAdminOnly        = 7_004;
	private const long ChAdminOnly         = 101_003;

	private const long UserChNotFound      = 7_005;
	private const long ChNotFound          = 101_005;

	private const long UserNotAMember      = 7_006;
	private const long ChNotAMember        = 101_006;

	private const long UserNoSend          = 7_007;
	private const long ChNoSend            = 101_007;

	private const long UserRateLimit       = 7_008;
	private const long SubRateLimit        = 7_009;
	private const long ChRateLimit         = 101_008;

	private const long UserRateLimitExpiry = 7_010;
	private const long SubRateLimitExpiry  = 7_011;
	private const long ChRateLimitExpiry   = 101_010;

	private const long UserRateLimitPerCh  = 7_012;
	private const long SubRateLimitPerChA  = 7_013;
	private const long SubRateLimitPerChB  = 7_014;
	private const long ChRateLimitA        = 101_012;
	private const long ChRateLimitB        = 101_013;

	private static readonly DateTimeOffset ClockAnchor =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	public Task InitializeAsync()
	{
		factory.GuildClient.Result = null;
		factory.UserClient.Reset();
		factory.Clock.Set(ClockAnchor);
		return Task.CompletedTask;
	}

	public Task DisposeAsync() => Task.CompletedTask;

	// T4.01 & T4.03 & T4.04 — subscriber receives TypingStarted with correct
	// payload; sender (also in group) does not receive
	[Fact]
	public async Task Typing_ValidChannel_SubscriberReceivesTypingStarted_SenderDoesNot()
	{
		factory.GuildClient.Result = new ChannelMembership(
			IsMember: true, GuildId: 5, Permissions: SendMessages | ReadMessages);

		var senderToken = TestTokens.Issue(ChatApiFactory.JwtSecret, UserHappyPath);
		var subToken    = TestTokens.Issue(ChatApiFactory.JwtSecret, SubHappyPath);

		await using var sender     = HubConnectionHelper.Build(factory, senderToken);
		await using var subscriber = HubConnectionHelper.Build(factory, subToken);

		var subReceived     = new TaskCompletionSource<(string u, string s, string i, DateTimeOffset e)>();
		var senderReceived  = 0;

		subscriber.On<string, string, string, DateTimeOffset>("TypingStarted",
			(u, s, i, e) => subReceived.TrySetResult((u, s, i, e)));
		sender.On<string, string, string, DateTimeOffset>("TypingStarted",
			(_, _, _, _) => Interlocked.Increment(ref senderReceived));

		await subscriber.StartAsync();
		await subscriber.InvokeAsync("JoinChannel", ChHappyPath);

		await sender.StartAsync();
		await sender.InvokeAsync("JoinChannel", ChHappyPath);

		await sender.InvokeAsync("Typing", "channel", ChHappyPath);

		var (userId, scope, id, expiresAt) =
			await subReceived.Task.WaitAsync(TimeSpan.FromSeconds(3));

		Assert.Equal(UserHappyPath.ToString(), userId);
		Assert.Equal("channel", scope);
		Assert.Equal(ChHappyPath.ToString(), id);
		Assert.Equal(ClockAnchor.AddSeconds(8), expiresAt);
		Assert.Equal(0, senderReceived);
	}

	// T4.02 — ADMINISTRATOR bit alone grants send access
	[Fact]
	public async Task Typing_AdministratorPermission_SubscriberReceivesTypingStarted()
	{
		factory.GuildClient.Result = new ChannelMembership(
			IsMember: true, GuildId: 5, Permissions: Administrator);

		var senderToken = TestTokens.Issue(ChatApiFactory.JwtSecret, UserAdminOnly);
		var subToken    = TestTokens.Issue(ChatApiFactory.JwtSecret, SubAdminOnly);

		await using var sender     = HubConnectionHelper.Build(factory, senderToken);
		await using var subscriber = HubConnectionHelper.Build(factory, subToken);

		var tcs = new TaskCompletionSource<bool>();
		subscriber.On<string, string, string, DateTimeOffset>("TypingStarted",
			(_, _, _, _) => tcs.TrySetResult(true));

		await subscriber.StartAsync();
		await subscriber.InvokeAsync("JoinChannel", ChAdminOnly);

		await sender.StartAsync();
		await sender.InvokeAsync("Typing", "channel", ChAdminOnly);

		await tcs.Task.WaitAsync(TimeSpan.FromSeconds(3));
	}

	// T4.05 — membership null → Error("Message.ChannelNotFound", ...)
	[Fact]
	public async Task Typing_ChannelNotFound_SendsError()
	{
		factory.GuildClient.Result = null;

		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserChNotFound);
		await using var conn = HubConnectionHelper.Build(factory, token);

		var errTcs = new TaskCompletionSource<string>();
		conn.On<string, string>("Error", (code, _) => errTcs.TrySetResult(code));

		await conn.StartAsync();
		await conn.InvokeAsync("Typing", "channel", ChNotFound);

		var code = await errTcs.Task.WaitAsync(TimeSpan.FromSeconds(3));
		Assert.Equal("Message.ChannelNotFound", code);
	}

	// T4.06 — Not a member → Error("Message.NotAMember", ...)
	[Fact]
	public async Task Typing_NotAMember_SendsError()
	{
		factory.GuildClient.Result = new ChannelMembership(
			IsMember: false, GuildId: 5, Permissions: SendMessages | ReadMessages);

		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserNotAMember);
		await using var conn = HubConnectionHelper.Build(factory, token);

		var errTcs = new TaskCompletionSource<string>();
		conn.On<string, string>("Error", (code, _) => errTcs.TrySetResult(code));

		await conn.StartAsync();
		await conn.InvokeAsync("Typing", "channel", ChNotAMember);

		var code = await errTcs.Task.WaitAsync(TimeSpan.FromSeconds(3));
		Assert.Equal("Message.NotAMember", code);
	}

	// T4.07 — No permissions → Error("Message.MissingSendPermission", ...)
	[Fact]
	public async Task Typing_MissingSendPermission_SendsError()
	{
		factory.GuildClient.Result = new ChannelMembership(
			IsMember: true, GuildId: 5, Permissions: ReadMessages);

		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserNoSend);
		await using var conn = HubConnectionHelper.Build(factory, token);

		var errTcs = new TaskCompletionSource<string>();
		conn.On<string, string>("Error", (code, _) => errTcs.TrySetResult(code));

		await conn.StartAsync();
		await conn.InvokeAsync("Typing", "channel", ChNoSend);

		var code = await errTcs.Task.WaitAsync(TimeSpan.FromSeconds(3));
		Assert.Equal("Message.MissingSendPermission", code);
	}

	// T4.08 — second Typing within the 3-second window is silently ignored
	[Fact]
	public async Task Typing_SecondCallWithinRateLimitWindow_IsIgnored()
	{
		factory.GuildClient.Result = new ChannelMembership(
			IsMember: true, GuildId: 5, Permissions: SendMessages | ReadMessages);

		var senderToken = TestTokens.Issue(ChatApiFactory.JwtSecret, UserRateLimit);
		var subToken    = TestTokens.Issue(ChatApiFactory.JwtSecret, SubRateLimit);

		await using var sender     = HubConnectionHelper.Build(factory, senderToken);
		await using var subscriber = HubConnectionHelper.Build(factory, subToken);

		var count = 0;
		subscriber.On<string, string, string, DateTimeOffset>("TypingStarted",
			(_, _, _, _) => Interlocked.Increment(ref count));

		await subscriber.StartAsync();
		await subscriber.InvokeAsync("JoinChannel", ChRateLimit);

		await sender.StartAsync();

		await sender.InvokeAsync("Typing", "channel", ChRateLimit);
		await Task.Delay(100);

		await sender.InvokeAsync("Typing", "channel", ChRateLimit); // rate limited
		await Task.Delay(200);

		Assert.Equal(1, count);
	}

	// T4.09 — after the 3-second window elapses, Typing works again
	[Fact]
	public async Task Typing_AfterRateLimitWindowExpires_WorksAgain()
	{
		factory.GuildClient.Result = new ChannelMembership(
			IsMember: true, GuildId: 5, Permissions: SendMessages | ReadMessages);

		var senderToken = TestTokens.Issue(ChatApiFactory.JwtSecret, UserRateLimitExpiry);
		var subToken    = TestTokens.Issue(ChatApiFactory.JwtSecret, SubRateLimitExpiry);

		await using var sender     = HubConnectionHelper.Build(factory, senderToken);
		await using var subscriber = HubConnectionHelper.Build(factory, subToken);

		var count = 0;
		var tcs1  = new TaskCompletionSource<bool>();
		var tcs2  = new TaskCompletionSource<bool>();
		subscriber.On<string, string, string, DateTimeOffset>("TypingStarted", (_, _, _, _) =>
		{
			var c = Interlocked.Increment(ref count);
			if (c == 1) tcs1.TrySetResult(true);
			if (c == 2) tcs2.TrySetResult(true);
		});

		await subscriber.StartAsync();
		await subscriber.InvokeAsync("JoinChannel", ChRateLimitExpiry);
		await sender.StartAsync();

		await sender.InvokeAsync("Typing", "channel", ChRateLimitExpiry);
		await tcs1.Task.WaitAsync(TimeSpan.FromSeconds(3));

		factory.Clock.Advance(TimeSpan.FromSeconds(3)); // expire the rate limit window

		await sender.InvokeAsync("Typing", "channel", ChRateLimitExpiry);
		await tcs2.Task.WaitAsync(TimeSpan.FromSeconds(3));

		Assert.Equal(2, count);
	}

	// T4.10 — rate limit key includes channelId; different channel is unaffected
	[Fact]
	public async Task Typing_RateLimitIsPerChannelId_OtherChannelNotAffected()
	{
		factory.GuildClient.Result = new ChannelMembership(
			IsMember: true, GuildId: 5, Permissions: SendMessages | ReadMessages);

		var senderToken = TestTokens.Issue(ChatApiFactory.JwtSecret, UserRateLimitPerCh);
		var subAToken   = TestTokens.Issue(ChatApiFactory.JwtSecret, SubRateLimitPerChA);
		var subBToken   = TestTokens.Issue(ChatApiFactory.JwtSecret, SubRateLimitPerChB);

		await using var sender = HubConnectionHelper.Build(factory, senderToken);
		await using var subA   = HubConnectionHelper.Build(factory, subAToken);
		await using var subB   = HubConnectionHelper.Build(factory, subBToken);

		var countA = 0;
		var tcsB   = new TaskCompletionSource<bool>();
		subA.On<string, string, string, DateTimeOffset>("TypingStarted",
			(_, _, _, _) => Interlocked.Increment(ref countA));
		subB.On<string, string, string, DateTimeOffset>("TypingStarted",
			(_, _, _, _) => tcsB.TrySetResult(true));

		await subA.StartAsync();
		await subA.InvokeAsync("JoinChannel", ChRateLimitA);

		await subB.StartAsync();
		await subB.InvokeAsync("JoinChannel", ChRateLimitB);

		await sender.StartAsync();

		// first call on ChA → rate limit set for (UserRateLimitPerCh, "channel", ChA)
		await sender.InvokeAsync("Typing", "channel", ChRateLimitA);
		await Task.Delay(100);

		// second call on ChA → silently ignored by rate limit
		await sender.InvokeAsync("Typing", "channel", ChRateLimitA);
		await Task.Delay(100);

		// call on ChB → different key, not affected by ChA's rate limit
		await sender.InvokeAsync("Typing", "channel", ChRateLimitB);
		await tcsB.Task.WaitAsync(TimeSpan.FromSeconds(3));

		Assert.Equal(1, countA); // only the first ChA call went through
	}
}

// ─────────────────────────────────────────────────────────────────────────────
// T4.11 – T4.15 · Typing — DM scope
// ─────────────────────────────────────────────────────────────────────────────
public sealed class DmTypingTests(ChatApiFactory factory)
	: IClassFixture<ChatApiFactory>, IAsyncLifetime
{
	private const long UserDmSender      = 7_101;
	private const long UserDmPartner     = 7_102;
	private const long UserDmSenderNF    = 7_103;
	private const long UserDmPartnerNF   = 7_104;
	private const long UserDmSenderBlock = 7_105;
	private const long UserDmPartnerBlock = 7_106;
	private const long UserDmSenderPend  = 7_107;
	private const long UserDmPartnerPend = 7_108;

	private static readonly DateTimeOffset ClockAnchor =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	public Task InitializeAsync()
	{
		factory.GuildClient.Result = null;
		factory.UserClient.Reset();
		factory.Clock.Set(ClockAnchor);
		return Task.CompletedTask;
	}

	public Task DisposeAsync() => Task.CompletedTask;

	// T4.11 — accepted relationship → partner receives TypingStarted
	[Fact]
	public async Task Typing_DmAccepted_PartnerReceivesTypingStarted()
	{
		factory.UserClient.Setup(UserDmSender, UserDmPartner,
			new UsersRelationship("accepted", ClockAnchor));

		var senderToken  = TestTokens.Issue(ChatApiFactory.JwtSecret, UserDmSender);
		var partnerToken = TestTokens.Issue(ChatApiFactory.JwtSecret, UserDmPartner);

		await using var sender  = HubConnectionHelper.Build(factory, senderToken);
		await using var partner = HubConnectionHelper.Build(factory, partnerToken);

		var tcs = new TaskCompletionSource<(string userId, string scope, string id)>();
		partner.On<string, string, string, DateTimeOffset>("TypingStarted",
			(u, s, i, _) => tcs.TrySetResult((u, s, i)));

		await partner.StartAsync();
		await sender.StartAsync();

		await sender.InvokeAsync("Typing", "dm", UserDmPartner);

		var (userId, scope, id) = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(3));
		Assert.Equal(UserDmSender.ToString(), userId);
		Assert.Equal("dm", scope);
		Assert.Equal(UserDmPartner.ToString(), id);
	}

	// T4.12 — relationship not found → Error("Message.UserNotFound", ...)
	[Fact]
	public async Task Typing_DmRelationshipNotFound_SendsError()
	{
		// no Setup → FakeUserClient returns null
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserDmSenderNF);
		await using var conn = HubConnectionHelper.Build(factory, token);

		var errTcs = new TaskCompletionSource<string>();
		conn.On<string, string>("Error", (code, _) => errTcs.TrySetResult(code));

		await conn.StartAsync();
		await conn.InvokeAsync("Typing", "dm", UserDmPartnerNF);

		var code = await errTcs.Task.WaitAsync(TimeSpan.FromSeconds(3));
		Assert.Equal("Message.UserNotFound", code);
	}

	// T4.13 & T4.14 — blocked relationship → Error("Message.RecipientBlocked", ...)
	[Theory]
	[InlineData("blocked_by_me")]
	[InlineData("blocked_by_them")]
	public async Task Typing_DmBlocked_SendsError(string status)
	{
		// blocked tests share the same sender ID — use different partner IDs per status
		// to avoid rate-limit collision on the static dict key (userId, "dm", partnerId)
		var partnerId = status == "blocked_by_me" ? UserDmPartnerBlock : UserDmPartnerBlock + 1;
		factory.UserClient.Setup(UserDmSenderBlock, partnerId,
			new UsersRelationship(status, ClockAnchor));

		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserDmSenderBlock);
		await using var conn = HubConnectionHelper.Build(factory, token);

		var errTcs = new TaskCompletionSource<string>();
		conn.On<string, string>("Error", (code, _) => errTcs.TrySetResult(code));

		await conn.StartAsync();
		await conn.InvokeAsync("Typing", "dm", partnerId);

		var code = await errTcs.Task.WaitAsync(TimeSpan.FromSeconds(3));
		Assert.Equal("Message.RecipientBlocked", code);
	}

	// T4.15 — pending relationship → Error("Message.RecipientNotFriend", ...)
	[Fact]
	public async Task Typing_DmPending_SendsError()
	{
		factory.UserClient.Setup(UserDmSenderPend, UserDmPartnerPend,
			new UsersRelationship("pending", ClockAnchor));

		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserDmSenderPend);
		await using var conn = HubConnectionHelper.Build(factory, token);

		var errTcs = new TaskCompletionSource<string>();
		conn.On<string, string>("Error", (code, _) => errTcs.TrySetResult(code));

		await conn.StartAsync();
		await conn.InvokeAsync("Typing", "dm", UserDmPartnerPend);

		var code = await errTcs.Task.WaitAsync(TimeSpan.FromSeconds(3));
		Assert.Equal("Message.RecipientNotFriend", code);
	}
}

// ─────────────────────────────────────────────────────────────────────────────
// T4.16 – T4.17 · Typing — invalid scope
// ─────────────────────────────────────────────────────────────────────────────
public sealed class TypingScopeTests(ChatApiFactory factory)
	: IClassFixture<ChatApiFactory>, IAsyncLifetime
{
	private const long UserInvalidScope = 7_201;

	public Task InitializeAsync()
	{
		factory.GuildClient.Result = null;
		factory.UserClient.Reset();
		return Task.CompletedTask;
	}

	public Task DisposeAsync() => Task.CompletedTask;

	// T4.16 & T4.17 — any scope other than "channel" or "dm" → Error("Typing.InvalidScope", ...)
	[Theory]
	[InlineData("guild")]
	[InlineData("")]
	public async Task Typing_InvalidScope_SendsError(string scope)
	{
		// use different target IDs per scope string to isolate rate-limit keys
		var targetId = scope == "guild" ? 201_001L : 201_002L;

		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, UserInvalidScope);
		await using var conn = HubConnectionHelper.Build(factory, token);

		var errTcs = new TaskCompletionSource<string>();
		conn.On<string, string>("Error", (code, _) => errTcs.TrySetResult(code));

		await conn.StartAsync();
		await conn.InvokeAsync("Typing", scope, targetId);

		var code = await errTcs.Task.WaitAsync(TimeSpan.FromSeconds(3));
		Assert.Equal("Typing.InvalidScope", code);
	}
}
