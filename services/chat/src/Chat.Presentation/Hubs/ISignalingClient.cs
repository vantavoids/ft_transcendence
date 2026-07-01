using Chat.Presentation.Hubs.Signaling;

namespace Chat.Presentation.Hubs;

public interface ISignalingClient
{
	Task IncomingCall(IncomingCallEvent evt);
	Task CallAnswered(CallAnsweredEvent evt);
	Task CallRejected(CallIdEvent evt);
	Task CallHungUp(CallIdEvent evt);
	Task CallFailed(CallFailedEvent evt);
	Task IceCandidate(IceCandidateEvent evt);
}
