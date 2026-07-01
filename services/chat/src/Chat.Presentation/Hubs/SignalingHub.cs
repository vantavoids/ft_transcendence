using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Authentication;
using Chat.Application.Contracts;
using Chat.Presentation.Hubs.Signaling;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Chat.Presentation.Hubs;

[Authorize]
public sealed class SignalingHub(
	ICurrentUser currentUser,
	CallRegistry calls,
	IEventBus eventBus) : Hub<ISignalingClient>
{
	public override async Task OnConnectedAsync()
	{
		calls.Connect(currentUser.UserId);
		foreach (var call in calls.PendingFor(currentUser.UserId))
			await Clients.Caller.IncomingCall(new IncomingCallEvent(
				call.CallId.ToString(), call.CallerId.ToString(), call.CallType, call.Sdp));
		await base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		calls.Disconnect(currentUser.UserId);
		await base.OnDisconnectedAsync(exception);
	}

	public async Task CallOffer(CallOfferArgs args)
	{
		if (!long.TryParse(args.CalleeId, out var calleeId) || !long.TryParse(args.CallId, out var callId))
			return;

		var callerId = currentUser.UserId;
		var info = new CallInfo(callId, callerId, calleeId, args.CallType, args.Sdp);

		if (!calls.TryOffer(info))
		{
			await Clients.Caller.CallFailed(new CallFailedEvent(args.CallId, "user_busy"));
			return;
		}

		if (calls.IsConnected(calleeId))
			await Clients.User(calleeId.ToString()).IncomingCall(new IncomingCallEvent(
				args.CallId, callerId.ToString(), args.CallType, args.Sdp));
		else
			await eventBus.PublishAsync(
				new CallIncoming(callId, callerId, calleeId, args.CallType), Context.ConnectionAborted);
	}

	public async Task CallAnswer(CallAnswerArgs args)
	{
		if (!long.TryParse(args.CallId, out var callId))
			return;

		var call = calls.Answer(callId, currentUser.UserId);
		if (call is null)
			return;

		await Clients.User(call.CallerId.ToString()).CallAnswered(new CallAnsweredEvent(args.CallId, args.Sdp));
	}

	public async Task CallReject(CallIdArgs args)
	{
		if (!long.TryParse(args.CallId, out var callId))
			return;

		var call = calls.End(callId, currentUser.UserId);
		if (call is null)
			return;

		await Clients.User(Other(call, currentUser.UserId).ToString())
			.CallRejected(new CallIdEvent(args.CallId));
	}

	public async Task CallHangup(CallIdArgs args)
	{
		if (!long.TryParse(args.CallId, out var callId))
			return;

		var call = calls.End(callId, currentUser.UserId);
		if (call is null)
			return;

		await Clients.User(Other(call, currentUser.UserId).ToString())
			.CallHungUp(new CallIdEvent(args.CallId));
	}

	public async Task IceCandidate(IceCandidateArgs args)
	{
		if (!long.TryParse(args.CallId, out var callId))
			return;

		var call = calls.ForParticipant(callId, currentUser.UserId);
		if (call is null)
			return;

		await Clients.User(Other(call, currentUser.UserId).ToString())
			.IceCandidate(new IceCandidateEvent(args.CallId, args.Candidate, args.SdpMid, args.SdpMlineIndex));
	}

	private static long Other(CallInfo call, long self) =>
		call.CallerId == self ? call.CalleeId : call.CallerId;
}
