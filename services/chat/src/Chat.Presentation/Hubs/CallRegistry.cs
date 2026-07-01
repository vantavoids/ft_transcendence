using Chat.Presentation.Hubs.Signaling;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Chat.Presentation.Hubs;

public sealed record CallInfo(long CallId, long CallerId, long CalleeId, string CallType, string Sdp);

/// <summary>
/// in-memory state for 1-on-1 WebRTC calls: signaling-hub presence, pending/active
/// call bookkeeping (a user is in at most one call), and the unanswered-call
/// timeout. single-node only, matching the peer-to-peer, et pour le meilleur
/// </summary>
public sealed class CallRegistry(
	IHubContext<SignalingHub, ISignalingClient> hub,
	IOptions<SignalingOptions> options,
	ILogger<CallRegistry> logger)
{
	private sealed class Entry
	{
		public required CallInfo Info { get; init; }
		public bool Ringing { get; set; } = true;
		public Timer? Timer { get; set; }
	}

	private readonly TimeSpan _answerTimeout = TimeSpan.FromSeconds(options.Value.AnswerTimeoutSeconds);
	private readonly Lock _gate = new();
	private readonly Dictionary<long, Entry> _byCall = [];
	private readonly Dictionary<long, long> _userCall = [];   // participant userId -> callId
	private readonly Dictionary<long, int> _connected = [];   // signaling-hub connection counts

	// ---- signaling-hub presence ----

	public void Connect(long userId)
	{
		lock (_gate)
			_connected[userId] = _connected.GetValueOrDefault(userId) + 1;
	}

	public void Disconnect(long userId)
	{
		lock (_gate)
		{
			if (!_connected.TryGetValue(userId, out var count))
				return;
			if (count <= 1)
				_connected.Remove(userId);
			else
				_connected[userId] = count - 1;
		}
	}

	public bool IsConnected(long userId)
	{
		lock (_gate)
			return _connected.ContainsKey(userId);
	}

	// ringing calls where the user is the callee, delivered as IncomingCall when
	// they (re)connect to the signaling hub.
	public IReadOnlyList<CallInfo> PendingFor(long calleeId)
	{
		lock (_gate)
			return _byCall.Values
				.Where(e => e.Ringing && e.Info.CalleeId == calleeId)
				.Select(e => e.Info)
				.ToList();
	}

	// ---- call lifecycle ----

	// registers a ringing call. returns false (user_busy) if either participant is
	// already in a call, which keeps the one-call-per-user invariant intact.
	public bool TryOffer(CallInfo info)
	{
		lock (_gate)
		{
			if (_userCall.ContainsKey(info.CallerId) || _userCall.ContainsKey(info.CalleeId))
				return false;

			var entry = new Entry { Info = info };
			_byCall[info.CallId] = entry;
			_userCall[info.CallerId] = info.CallId;
			_userCall[info.CalleeId] = info.CallId;
			entry.Timer = new Timer(OnTimeout, info.CallId, _answerTimeout, Timeout.InfiniteTimeSpan);
			return true;
		}
	}

	// callee accepts: cancels the timeout and marks the call active. returns the
	// call (to relay CallAnswered to the caller), or null if the answer is invalid.
	public CallInfo? Answer(long callId, long byUserId)
	{
		lock (_gate)
		{
			if (!_byCall.TryGetValue(callId, out var entry) || !entry.Ringing || entry.Info.CalleeId != byUserId)
				return null;

			entry.Ringing = false;
			entry.Timer?.Dispose();
			entry.Timer = null;
			return entry.Info;
		}
	}

	// ends a call (hangup or reject) by a participant and clears its state. returns
	// the call so the caller can notify the other party, or null if invalid.
	public CallInfo? End(long callId, long byUserId)
	{
		lock (_gate)
		{
			if (!_byCall.TryGetValue(callId, out var entry) ||
				(entry.Info.CallerId != byUserId && entry.Info.CalleeId != byUserId))
				return null;

			RemoveLocked(callId);
			return entry.Info;
		}
	}

	// for ICE relay: returns the call only if the user is one of its participants.
	public CallInfo? ForParticipant(long callId, long userId)
	{
		lock (_gate)
		{
			if (!_byCall.TryGetValue(callId, out var entry) ||
				(entry.Info.CallerId != userId && entry.Info.CalleeId != userId))
				return null;

			return entry.Info;
		}
	}

	private void RemoveLocked(long callId)
	{
		if (!_byCall.Remove(callId, out var entry))
			return;
		entry.Timer?.Dispose();
		_userCall.Remove(entry.Info.CallerId);
		_userCall.Remove(entry.Info.CalleeId);
	}

	private void OnTimeout(object? state)
	{
		var callId = (long)state!;
		CallInfo? timedOut = null;
		lock (_gate)
		{
			if (_byCall.TryGetValue(callId, out var entry) && entry.Ringing)
			{
				timedOut = entry.Info;
				RemoveLocked(callId);
			}
		}

		if (timedOut is not null)
		{
			logger.LogInformation("Call {CallId}: unanswered after {Seconds}s, failing both parties",
				timedOut.CallId, _answerTimeout.TotalSeconds);
			_ = hub.Clients
				.Users([timedOut.CallerId.ToString(), timedOut.CalleeId.ToString()])
				.CallFailed(new CallFailedEvent(timedOut.CallId.ToString(), "timeout"));
		}
	}
}
