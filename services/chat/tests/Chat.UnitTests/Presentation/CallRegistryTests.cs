using Chat.Presentation.Hubs;
using Chat.Presentation.Hubs.Signaling;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Chat.UnitTests.Presentation;

public sealed class CallRegistryTests
{
	private const long Caller = 1;
	private const long Callee = 2;
	private const long Third = 3;
	private const long CallId = 900;

	private static CallInfo Offer(long callId = CallId, long caller = Caller, long callee = Callee) =>
		new(callId, caller, callee, "video", "sdp-offer");

	private static (CallRegistry Registry, RecordingSignalingHub Hub) NewRegistry(int timeoutSeconds = 3600)
	{
		var hub = new RecordingSignalingHub();
		var opts = Options.Create(new SignalingOptions { AnswerTimeoutSeconds = timeoutSeconds });
		return (new CallRegistry(hub, opts, NullLogger<CallRegistry>.Instance), hub);
	}

	[Fact]
	public void TryOffer_Succeeds_ThenSecondOfferToBusyParticipant_Fails()
	{
		var (registry, _) = NewRegistry();
		Assert.True(registry.TryOffer(Offer()));

		// another call where the callee is already busy
		Assert.False(registry.TryOffer(new CallInfo(901, Third, Callee, "audio", "x")));
		// caller already busy
		Assert.False(registry.TryOffer(new CallInfo(902, Caller, Third, "audio", "x")));
	}

	[Fact]
	public void TryOffer_RejectsDuplicateCallId()
	{
		var (registry, _) = NewRegistry();
		Assert.True(registry.TryOffer(Offer())); // call 900: Caller -> Callee

		// a free pair reusing the same (client-supplied) call_id must be rejected,
		// not overwrite the live call and strand its participants
		Assert.False(registry.TryOffer(new CallInfo(CallId, Third, 4, "audio", "x")));

		// the original call is intact: its real caller can still end it
		Assert.NotNull(registry.End(CallId, Caller));
	}

	[Fact]
	public void Answer_ByCallee_ReturnsCall_NonCalleeOrUnknown_ReturnsNull()
	{
		var (registry, _) = NewRegistry();
		registry.TryOffer(Offer());

		Assert.Null(registry.Answer(CallId, Third));       // not the callee
		Assert.Null(registry.Answer(999, Callee));          // unknown call
		var answered = registry.Answer(CallId, Callee);
		Assert.NotNull(answered);
		Assert.Equal(Caller, answered!.CallerId);
		Assert.Null(registry.Answer(CallId, Callee));       // already answered
	}

	[Fact]
	public void End_ByParticipant_ClearsState_AndFreesBothUsers()
	{
		var (registry, _) = NewRegistry();
		registry.TryOffer(Offer());

		Assert.Null(registry.End(CallId, Third));           // not a participant
		var ended = registry.End(CallId, Callee);           // callee rejects
		Assert.NotNull(ended);

		// both users are free to start new calls again
		Assert.True(registry.TryOffer(new CallInfo(903, Caller, Third, "audio", "x")));
		Assert.True(registry.TryOffer(new CallInfo(904, Callee, 4, "audio", "x")));
	}

	[Fact]
	public void Disconnect_AbandonsActiveCall_WhenLastConnectionDrops_FreeingBothUsers()
	{
		var (registry, _) = NewRegistry();
		registry.Connect(Caller);
		registry.Connect(Callee);
		registry.TryOffer(Offer());
		registry.Answer(CallId, Callee);   // active, no longer ringing

		// caller refreshes: their last connection drops and the call is abandoned
		var abandoned = registry.Disconnect(Caller);
		Assert.NotNull(abandoned);
		Assert.Equal(CallId, abandoned!.CallId);

		// both participants are free again; the now-stale callee disconnect is a no-op
		Assert.Null(registry.Disconnect(Callee));
		Assert.True(registry.TryOffer(new CallInfo(906, Caller, Third, "audio", "x")));
		Assert.True(registry.TryOffer(new CallInfo(907, Callee, 4, "audio", "x")));
	}

	[Fact]
	public void Disconnect_DoesNotAbandonCall_WhileOtherConnectionRemains()
	{
		var (registry, _) = NewRegistry();
		registry.Connect(Caller);
		registry.Connect(Caller);   // two tabs
		registry.TryOffer(Offer());

		Assert.Null(registry.Disconnect(Caller));   // one tab left, call stays
		Assert.False(registry.TryOffer(new CallInfo(908, Caller, Third, "audio", "x")));

		Assert.NotNull(registry.Disconnect(Caller)); // last tab gone, call abandoned
	}

	[Fact]
	public void CountsSnapshot_Reflects_Connections_And_Calls()
	{
		var (registry, _) = NewRegistry();
		Assert.Equal((0, 0, 0), registry.CountsSnapshot());

		registry.Connect(Caller);
		registry.Connect(Callee);
		Assert.Equal((0, 0, 2), registry.CountsSnapshot());   // two signaling users, no calls

		registry.TryOffer(Offer());
		Assert.Equal((1, 1, 2), registry.CountsSnapshot());   // one ringing call

		registry.Answer(CallId, Callee);
		Assert.Equal((1, 0, 2), registry.CountsSnapshot());   // now in progress, not ringing

		registry.End(CallId, Caller);
		Assert.Equal((0, 0, 2), registry.CountsSnapshot());   // call cleared

		registry.Disconnect(Caller);
		Assert.Equal((0, 0, 1), registry.CountsSnapshot());   // one signaling user left
	}

	[Fact]
	public void ForParticipant_ReturnsCall_OnlyForParticipants()
	{
		var (registry, _) = NewRegistry();
		registry.TryOffer(Offer());

		Assert.NotNull(registry.ForParticipant(CallId, Caller));
		Assert.NotNull(registry.ForParticipant(CallId, Callee));
		Assert.Null(registry.ForParticipant(CallId, Third));
	}

	[Fact]
	public void Presence_IsRefCounted()
	{
		var (registry, _) = NewRegistry();
		Assert.False(registry.IsConnected(Callee));

		registry.Connect(Callee);
		registry.Connect(Callee);
		Assert.True(registry.IsConnected(Callee));

		registry.Disconnect(Callee);
		Assert.True(registry.IsConnected(Callee));   // still one connection open
		registry.Disconnect(Callee);
		Assert.False(registry.IsConnected(Callee));
	}

	[Fact]
	public void PendingFor_ReturnsRingingCalls_ForCallee_UntilResolved()
	{
		var (registry, _) = NewRegistry();
		registry.TryOffer(Offer());

		Assert.Equal(CallId, Assert.Single(registry.PendingFor(Callee)).CallId);
		Assert.Empty(registry.PendingFor(Caller));   // caller is not a callee

		registry.Answer(CallId, Callee);
		Assert.Empty(registry.PendingFor(Callee));   // answered calls are no longer pending
	}

	[Fact]
	public async Task Timeout_Fires_CallFailed_ToCaller_WhenUnanswered()
	{
		var (registry, hub) = NewRegistry(timeoutSeconds: 1);
		registry.TryOffer(Offer());

		var failed = await hub.WaitForCallFailedAsync(TimeSpan.FromSeconds(4));

		Assert.NotNull(failed);
		Assert.Equal(Caller.ToString(), failed!.Value.UserId); // caller only, per contract
		Assert.Equal(CallId.ToString(), failed.Value.Evt.CallId);
		Assert.Equal("timeout", failed.Value.Evt.Reason);
		// state cleared: the caller can offer again
		Assert.True(registry.TryOffer(new CallInfo(905, Caller, Third, "audio", "x")));
	}

	[Fact]
	public async Task Timeout_DoesNotFire_AfterAnswer()
	{
		var (registry, hub) = NewRegistry(timeoutSeconds: 1);
		registry.TryOffer(Offer());
		registry.Answer(CallId, Callee);

		var failed = await hub.WaitForCallFailedAsync(TimeSpan.FromSeconds(2));
		Assert.Null(failed);
	}

	// records CallFailed sends; every other member is unused by the registry.
	private sealed class RecordingSignalingHub : IHubContext<SignalingHub, ISignalingClient>
	{
		private readonly List<(string UserId, CallFailedEvent Evt)> _sent = [];
		private readonly Lock _lock = new();

		public IHubClients<ISignalingClient> Clients => new Hubs(this);
		public IGroupManager Groups => throw new NotSupportedException();

		public async Task<(string UserId, CallFailedEvent Evt)?> WaitForCallFailedAsync(TimeSpan within)
		{
			var deadline = DateTime.UtcNow + within;
			while (DateTime.UtcNow < deadline)
			{
				lock (_lock)
					if (_sent.Count > 0)
						return _sent[0];
				await Task.Delay(25);
			}
			return null;
		}

		private void Record(string userId, CallFailedEvent evt)
		{
			lock (_lock)
				_sent.Add((userId, evt));
		}

		private sealed class Hubs(RecordingSignalingHub owner) : IHubClients<ISignalingClient>
		{
			public ISignalingClient User(string userId) => new Client(owner, userId);
			public ISignalingClient All => throw new NotSupportedException();
			public ISignalingClient AllExcept(IReadOnlyList<string> e) => throw new NotSupportedException();
			public ISignalingClient Client(string connectionId) => throw new NotSupportedException();
			public ISignalingClient Clients(IReadOnlyList<string> c) => throw new NotSupportedException();
			public ISignalingClient Group(string g) => throw new NotSupportedException();
			public ISignalingClient GroupExcept(string g, IReadOnlyList<string> e) => throw new NotSupportedException();
			public ISignalingClient Groups(IReadOnlyList<string> g) => throw new NotSupportedException();
			public ISignalingClient Users(IReadOnlyList<string> u) => throw new NotSupportedException();
		}

		private sealed class Client(RecordingSignalingHub owner, string userId) : ISignalingClient
		{
			public Task CallFailed(CallFailedEvent evt) { owner.Record(userId, evt); return Task.CompletedTask; }
			public Task IncomingCall(IncomingCallEvent evt) => Task.CompletedTask;
			public Task CallAnswered(CallAnsweredEvent evt) => Task.CompletedTask;
			public Task CallRejected(CallIdEvent evt) => Task.CompletedTask;
			public Task CallHungUp(CallIdEvent evt) => Task.CompletedTask;
			public Task IceCandidate(IceCandidateEvent evt) => Task.CompletedTask;
		}
	}
}
