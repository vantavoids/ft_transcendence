using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Chat.Presentation.Hubs;

[Authorize]
public sealed class SignalingHub : Hub<ISignalingClient>
{
	public Task SendOffer(long targetUserId, string sdp) =>
		Clients.User(targetUserId.ToString())
			.Offer(Context.UserIdentifier, sdp);

	public Task SendAnswer(long targetUserId, string sdp) =>
		Clients.User(targetUserId.ToString())
			.Answer(Context.UserIdentifier, sdp);

	public Task SendIceCandidate(long targetUserId, string candidate) =>
		Clients.User(targetUserId.ToString())
			.IceCandidate(Context.UserIdentifier, candidate);
}
