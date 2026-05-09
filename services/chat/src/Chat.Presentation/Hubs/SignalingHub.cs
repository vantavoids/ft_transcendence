using Microsoft.AspNetCore.SignalR;

namespace Chat.Presentation.Hubs;

public sealed class SignalingHub : Hub<ISignalingClient>
{
	public Task SendOffer(Guid targetUserId, string sdp) =>
		Clients.User(targetUserId.ToString())
			.Offer(Context.UserIdentifier, sdp);

	public Task SendAnswer(Guid targetUserId, string sdp) =>
		Clients.User(targetUserId.ToString())
			.Answer(Context.UserIdentifier, sdp);

	public Task SendIceCandidate(Guid targetUserId, string candidate) =>
		Clients.User(targetUserId.ToString())
			.IceCandidate(Context.UserIdentifier, candidate);
}
