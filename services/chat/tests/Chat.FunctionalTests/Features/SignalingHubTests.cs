using Chat.Application.Contracts;
using Chat.FunctionalTests.Infrastructure;
using Chat.Presentation.Hubs.Signaling;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

namespace Chat.FunctionalTests.Features;

// WebRTC signaling relay via /v1/hubs/signaling. Each test uses unique user/call
// ids so the process-wide CallRegistry singleton can't leak state between tests.
public sealed class SignalingHubTests(ChatApiFactory factory)
	: IClassFixture<ChatApiFactory>, IAsyncLifetime
{
	private const string SignalingPath = "/v1/hubs/signaling";
	private static readonly TimeSpan Wait = TimeSpan.FromSeconds(3);

	public Task InitializeAsync()
	{
		factory.EventBus.Reset();
		return Task.CompletedTask;
	}

	public Task DisposeAsync() => Task.CompletedTask;

	private HubConnection Connect(long userId) =>
		HubConnectionHelper.Build(factory, TestTokens.Issue(ChatApiFactory.JwtSecret, userId), SignalingPath);

	[Fact]
	public async Task Offer_WhenCalleeConnected_CalleeReceivesIncomingCall()
	{
		const long caller = 8_001, callee = 8_002, callId = 90_001;
		await using var callerConn = Connect(caller);
		await using var calleeConn = Connect(callee);

		var incoming = new TaskCompletionSource<IncomingCallEvent>();
		calleeConn.On<IncomingCallEvent>("IncomingCall", e => incoming.TrySetResult(e));

		await calleeConn.StartAsync();
		await callerConn.StartAsync();
		await callerConn.InvokeAsync("CallOffer",
			new CallOfferArgs(callee.ToString(), callId.ToString(), "video", "sdp-offer"));

		var e = await incoming.Task.WaitAsync(Wait);
		Assert.Equal(callId.ToString(), e.CallId);
		Assert.Equal(caller.ToString(), e.CallerId);
		Assert.Equal("video", e.CallType);
		Assert.Equal("sdp-offer", e.Sdp);
	}

	[Fact]
	public async Task Answer_CallerReceivesCallAnswered()
	{
		const long caller = 8_011, callee = 8_012, callId = 90_011;
		await using var callerConn = Connect(caller);
		await using var calleeConn = Connect(callee);

		var answered = new TaskCompletionSource<CallAnsweredEvent>();
		callerConn.On<CallAnsweredEvent>("CallAnswered", e => answered.TrySetResult(e));

		await callerConn.StartAsync();
		await calleeConn.StartAsync();
		await callerConn.InvokeAsync("CallOffer",
			new CallOfferArgs(callee.ToString(), callId.ToString(), "audio", "offer"));
		await calleeConn.InvokeAsync("CallAnswer", new CallAnswerArgs(callId.ToString(), "sdp-answer"));

		var e = await answered.Task.WaitAsync(Wait);
		Assert.Equal(callId.ToString(), e.CallId);
		Assert.Equal("sdp-answer", e.Sdp);
	}

	[Fact]
	public async Task Reject_CallerReceivesCallRejected()
	{
		const long caller = 8_021, callee = 8_022, callId = 90_021;
		await using var callerConn = Connect(caller);
		await using var calleeConn = Connect(callee);

		var rejected = new TaskCompletionSource<CallIdEvent>();
		callerConn.On<CallIdEvent>("CallRejected", e => rejected.TrySetResult(e));

		await callerConn.StartAsync();
		await calleeConn.StartAsync();
		await callerConn.InvokeAsync("CallOffer",
			new CallOfferArgs(callee.ToString(), callId.ToString(), "video", "offer"));
		await calleeConn.InvokeAsync("CallReject", new CallIdArgs(callId.ToString()));

		var e = await rejected.Task.WaitAsync(Wait);
		Assert.Equal(callId.ToString(), e.CallId);
	}

	[Fact]
	public async Task Hangup_OtherPartyReceivesCallHungUp()
	{
		const long caller = 8_031, callee = 8_032, callId = 90_031;
		await using var callerConn = Connect(caller);
		await using var calleeConn = Connect(callee);

		var hungUp = new TaskCompletionSource<CallIdEvent>();
		calleeConn.On<CallIdEvent>("CallHungUp", e => hungUp.TrySetResult(e));

		await callerConn.StartAsync();
		await calleeConn.StartAsync();
		await callerConn.InvokeAsync("CallOffer",
			new CallOfferArgs(callee.ToString(), callId.ToString(), "video", "offer"));
		await callerConn.InvokeAsync("CallHangup", new CallIdArgs(callId.ToString()));

		var e = await hungUp.Task.WaitAsync(Wait);
		Assert.Equal(callId.ToString(), e.CallId);
	}

	[Fact]
	public async Task IceCandidate_RelayedToOtherParty()
	{
		const long caller = 8_041, callee = 8_042, callId = 90_041;
		await using var callerConn = Connect(caller);
		await using var calleeConn = Connect(callee);

		var ice = new TaskCompletionSource<IceCandidateEvent>();
		calleeConn.On<IceCandidateEvent>("IceCandidate", e => ice.TrySetResult(e));

		await callerConn.StartAsync();
		await calleeConn.StartAsync();
		await callerConn.InvokeAsync("CallOffer",
			new CallOfferArgs(callee.ToString(), callId.ToString(), "video", "offer"));
		await callerConn.InvokeAsync("IceCandidate",
			new IceCandidateArgs(callId.ToString(), "candidate:1", "0", 0));

		var e = await ice.Task.WaitAsync(Wait);
		Assert.Equal(callId.ToString(), e.CallId);
		Assert.Equal("candidate:1", e.Candidate);
		Assert.Equal("0", e.SdpMid);
		Assert.Equal(0, e.SdpMlineIndex);
	}

	[Fact]
	public async Task Offer_WhenCalleeBusy_CallerReceivesUserBusy()
	{
		const long a = 8_051, b = 8_052, c = 8_053, call1 = 90_051, call2 = 90_052;
		await using var connA = Connect(a);
		await using var connB = Connect(b);
		await using var connC = Connect(c);

		var busy = new TaskCompletionSource<CallFailedEvent>();
		connC.On<CallFailedEvent>("CallFailed", e => busy.TrySetResult(e));

		await connA.StartAsync();
		await connB.StartAsync();
		await connC.StartAsync();

		await connA.InvokeAsync("CallOffer", new CallOfferArgs(b.ToString(), call1.ToString(), "video", "offer"));
		await connC.InvokeAsync("CallOffer", new CallOfferArgs(b.ToString(), call2.ToString(), "video", "offer"));

		var e = await busy.Task.WaitAsync(Wait);
		Assert.Equal(call2.ToString(), e.CallId);
		Assert.Equal("user_busy", e.Reason);
	}

	[Fact]
	public async Task Offer_WhenCalleeOffline_PublishesCallIncoming_AndDeliversOnConnect()
	{
		const long caller = 8_061, callee = 8_062, callId = 90_061;
		await using var callerConn = Connect(caller);
		await callerConn.StartAsync();

		// callee is not connected -> offline path
		await callerConn.InvokeAsync("CallOffer",
			new CallOfferArgs(callee.ToString(), callId.ToString(), "audio", "offer"));

		var published = Assert.Single(factory.EventBus.PublishedOf<CallIncoming>());
		Assert.Equal(callId, published.CallId);
		Assert.Equal(caller, published.CallerId);
		Assert.Equal(callee, published.CalleeId);

		// when the callee finally connects, the pending call is delivered
		await using var calleeConn = Connect(callee);
		var incoming = new TaskCompletionSource<IncomingCallEvent>();
		calleeConn.On<IncomingCallEvent>("IncomingCall", e => incoming.TrySetResult(e));
		await calleeConn.StartAsync();

		var e = await incoming.Task.WaitAsync(Wait);
		Assert.Equal(callId.ToString(), e.CallId);
		Assert.Equal(caller.ToString(), e.CallerId);
	}
}
