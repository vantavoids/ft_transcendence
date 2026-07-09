// Wire types for the WebRTC signaling relayed through the Chat SignalR hub.
// Mirrors docs/contracts/chat.md "WebRTC Signaling" verbatim. Snowflakes are
// serialized as quoted strings on the wire.

export type CallType = 'audio' | 'video';

export type CallFailedReason = 'user_busy' | 'timeout';

// Client -> Server invocations
export type CallOfferArgs = {
  callee_id: string;
  call_id: string;
  call_type: CallType;
  sdp: string;
};

export type CallAnswerArgs = {
  call_id: string;
  sdp: string;
};

// CallReject and CallHangup both carry only the call id
export type CallIdArgs = {
  call_id: string;
};

export type IceCandidatePayload = {
  call_id: string;
  candidate: string;
  sdp_mid: string | null;
  sdp_mline_index: number | null;
};

// Server -> Client events
export type IncomingCallEvent = {
  call_id: string;
  caller_id: string;
  call_type: CallType;
  sdp: string;
};

export type CallAnsweredEvent = {
  call_id: string;
  sdp: string;
};

export type CallRejectedEvent = {
  call_id: string;
};

export type CallHungUpEvent = {
  call_id: string;
};

export type CallFailedEvent = {
  call_id: string;
  reason: CallFailedReason;
};

// Local UI state machine
export type CallStatus = 'idle' | 'outgoing' | 'incoming' | 'active' | 'ended';

export type CallState = {
  status: CallStatus;
  callId: string | null;
  // the other party's user id
  peerId: string | null;
  callType: CallType;
  isMicMuted: boolean;
  isCameraOff: boolean;
  // human-readable reason a call ended/failed, shown briefly in the UI
  endedReason: string | null;
};
