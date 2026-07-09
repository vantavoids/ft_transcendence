using Chat.Application.Abstractions;
using Chat.Application.Contracts;
using Chat.Presentation.Hubs.Signaling;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Chat.Presentation.Hubs;

[Authorize]
public sealed class SignalingHub(
	CallRegistry calls,
	IEventBus eventBus,
	ILogger<SignalingHub> logger) : Hub<ISignalingClient>
{
	public override async Task OnConnectedAsync()
	{
		calls.Connect(Context.GetUserId());
		logger.LogDebug("Signaling: {User} connected", Context.GetUserId());
		foreach (var call in calls.PendingFor(Context.GetUserId()))
			await Clients.Caller.IncomingCall(new IncomingCallEvent(
				call.CallId.ToString(), call.CallerId.ToString(), call.CallType, call.Sdp));
		await base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		calls.Disconnect(Context.GetUserId());
		logger.LogDebug("Signaling: {User} disconnected", Context.GetUserId());
		await base.OnDisconnectedAsync(exception);
	}

	public async Task CallOffer(CallOfferArgs args)
	{
		if (!long.TryParse(args.CalleeId, out var calleeId) || !long.TryParse(args.CallId, out var callId))
			return;

		var callerId = Context.GetUserId();
		var info = new CallInfo(callId, callerId, calleeId, args.CallType, args.Sdp);

		if (!calls.TryOffer(info))
		{
			logger.LogInformation("Call {CallId}: {Caller} -> {Callee} rejected, peer busy", callId, callerId, calleeId);
			await Clients.Caller.CallFailed(new CallFailedEvent(args.CallId, "user_busy"));
			return;
		}

		if (calls.IsConnected(calleeId))
		{
			logger.LogInformation("Call {CallId}: relaying offer {Caller} -> {Callee} (callee online)", callId, callerId, calleeId);
			await Clients.User(calleeId.ToString()).IncomingCall(new IncomingCallEvent(
				args.CallId, callerId.ToString(), args.CallType, args.Sdp));
		}
		else
		{
			logger.LogInformation("Call {CallId}: {Callee} offline, publishing call.incoming", callId, calleeId);
			await eventBus.PublishAsync(
				new CallIncoming(callId, callerId, calleeId, args.CallType), Context.ConnectionAborted);
		}
	}

	public async Task CallAnswer(CallAnswerArgs args)
	{
		if (!long.TryParse(args.CallId, out var callId))
			return;

		var call = calls.Answer(callId, Context.GetUserId());
		if (call is null)
			return;

		logger.LogInformation("Call {CallId}: answered by {Callee}", callId, Context.GetUserId());
		await Clients.User(call.CallerId.ToString()).CallAnswered(new CallAnsweredEvent(args.CallId, args.Sdp));
	}

	public async Task CallReject(CallIdArgs args)
	{
		if (!long.TryParse(args.CallId, out var callId))
			return;

		var call = calls.End(callId, Context.GetUserId());
		if (call is null)
			return;

		logger.LogInformation("Call {CallId}: rejected by {User}", callId, Context.GetUserId());
		await Clients.User(Other(call, Context.GetUserId()).ToString())
			.CallRejected(new CallIdEvent(args.CallId));
	}

	public async Task CallHangup(CallIdArgs args)
	{
		if (!long.TryParse(args.CallId, out var callId))
			return;

		var call = calls.End(callId, Context.GetUserId());
		if (call is null)
			return;

		logger.LogInformation("Call {CallId}: hung up by {User}", callId, Context.GetUserId());
		await Clients.User(Other(call, Context.GetUserId()).ToString())
			.CallHungUp(new CallIdEvent(args.CallId));
	}

	public async Task IceCandidate(IceCandidateArgs args)
	{
		if (!long.TryParse(args.CallId, out var callId))
			return;

		var call = calls.ForParticipant(callId, Context.GetUserId());
		if (call is null)
			return;

		await Clients.User(Other(call, Context.GetUserId()).ToString())
			.IceCandidate(new IceCandidateEvent(args.CallId, args.Candidate, args.SdpMid, args.SdpMlineIndex));
	}

	private static long Other(CallInfo call, long self) =>
		call.CallerId == self ? call.CalleeId : call.CallerId;
}
