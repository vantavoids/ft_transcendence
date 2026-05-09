namespace Chat.Presentation.Hubs;

public interface ISignalingClient
{
	Task Offer(string? from, string sdp);
	Task Answer(string? from, string sdp);
	Task IceCandidate(string? from, string candidate);
}
