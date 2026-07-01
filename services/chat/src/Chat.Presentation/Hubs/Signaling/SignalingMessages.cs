using System.Text.Json.Serialization;

namespace Chat.Presentation.Hubs.Signaling;

// wire shapes for WebRTC signaling. snowflake ids are quoted strings and fields
// are snake_case to match docs/contracts/chat.md (SignalR's default protocol is
// camelCase, so the names are pinned explicitly).

// ---- Client -> Server (invocation arguments) ----

public sealed record CallOfferArgs(
	[property: JsonPropertyName("callee_id")] string CalleeId,
	[property: JsonPropertyName("call_id")] string CallId,
	[property: JsonPropertyName("call_type")] string CallType,
	[property: JsonPropertyName("sdp")] string Sdp);

public sealed record CallAnswerArgs(
	[property: JsonPropertyName("call_id")] string CallId,
	[property: JsonPropertyName("sdp")] string Sdp);

public sealed record CallIdArgs(
	[property: JsonPropertyName("call_id")] string CallId);

public sealed record IceCandidateArgs(
	[property: JsonPropertyName("call_id")] string CallId,
	[property: JsonPropertyName("candidate")] string Candidate,
	[property: JsonPropertyName("sdp_mid")] string? SdpMid,
	[property: JsonPropertyName("sdp_mline_index")] int SdpMlineIndex);

// ---- Server -> Client (event payloads) ----

public sealed record IncomingCallEvent(
	[property: JsonPropertyName("call_id")] string CallId,
	[property: JsonPropertyName("caller_id")] string CallerId,
	[property: JsonPropertyName("call_type")] string CallType,
	[property: JsonPropertyName("sdp")] string Sdp);

public sealed record CallAnsweredEvent(
	[property: JsonPropertyName("call_id")] string CallId,
	[property: JsonPropertyName("sdp")] string Sdp);

public sealed record CallIdEvent(
	[property: JsonPropertyName("call_id")] string CallId);

public sealed record CallFailedEvent(
	[property: JsonPropertyName("call_id")] string CallId,
	[property: JsonPropertyName("reason")] string Reason);

public sealed record IceCandidateEvent(
	[property: JsonPropertyName("call_id")] string CallId,
	[property: JsonPropertyName("candidate")] string Candidate,
	[property: JsonPropertyName("sdp_mid")] string? SdpMid,
	[property: JsonPropertyName("sdp_mline_index")] int SdpMlineIndex);
