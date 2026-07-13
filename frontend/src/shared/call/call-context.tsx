'use client';

import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react';
import { useSignalingHub } from '../realtime/signaling-hub';
import { getIceServers } from './ice';
import type {
  CallAnsweredEvent,
  CallFailedEvent,
  CallHungUpEvent,
  CallRejectedEvent,
  CallState,
  CallType,
  IceCandidatePayload,
  IncomingCallEvent
} from './types';

const IDLE_STATE: CallState = {
  status: 'idle',
  callId: null,
  peerId: null,
  callType: 'video',
  isMicMuted: false,
  isCameraOff: false,
  endedReason: null
};

// how long the "ended" banner lingers before returning to idle
const ENDED_LINGER_MS = 2500;

// auto-dismiss a still-ringing incoming call. The server abandons an unanswered
// call after AnswerTimeoutSeconds (120s) but only notifies the caller, so the
// callee must time its own ring out or the overlay lingers forever.
const RING_TIMEOUT_MS = 120_000;

type CallContextValue = {
  state: CallState;
  localStream: MediaStream | null;
  remoteStream: MediaStream | null;
  startCall: (peerId: string, callType: CallType) => Promise<void>;
  acceptCall: () => Promise<void>;
  rejectCall: () => void;
  hangUp: () => void;
  toggleMic: () => void;
  toggleCamera: () => void;
};

const CallContext = createContext<CallContextValue | null>(null);

// Caller generates the call id (docs/contracts/chat.md). We produce a
// snowflake-shaped decimal string; uniqueness comes from time + jitter.
function generateCallId(): string {
  return (BigInt(Date.now()) * 1000n + BigInt(Math.floor(Math.random() * 1000))).toString();
}

export function CallProvider({ children }: { children: React.ReactNode }) {
  const { on, off, invoke } = useSignalingHub();

  const [state, setState] = useState<CallState>(IDLE_STATE);
  const [localStream, setLocalStream] = useState<MediaStream | null>(null);
  const [remoteStream, setRemoteStream] = useState<MediaStream | null>(null);

  const pcRef = useRef<RTCPeerConnection | null>(null);
  const localStreamRef = useRef<MediaStream | null>(null);
  const callIdRef = useRef<string | null>(null);
  const pendingOfferRef = useRef<IncomingCallEvent | null>(null);
  const pendingCandidatesRef = useRef<RTCIceCandidateInit[]>([]);
  const remoteDescSetRef = useRef(false);
  const liveRef = useRef(false);
  const endedTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const ringTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // keep the latest invoke reachable from stable (dep-free) callbacks
  const invokeRef = useRef(invoke);
  useEffect(() => {
    invokeRef.current = invoke;
  });

  // fire-and-forget signaling; a dropped hub relay must never throw into the UI
  const send = useCallback((method: string, payload: unknown) => {
    invokeRef.current(method, payload).catch(() => {
      /* hub not connected / relay dropped */
    });
  }, []);

  const teardownMedia = useCallback(() => {
    pcRef.current?.close();
    pcRef.current = null;
    localStreamRef.current?.getTracks().forEach((track) => track.stop());
    localStreamRef.current = null;
    pendingCandidatesRef.current = [];
    remoteDescSetRef.current = false;
    setLocalStream(null);
    setRemoteStream(null);
  }, []);

  // Wind a call down. `reason` (if given) is shown briefly before returning idle.
  const endCall = useCallback(
    (reason: string | null) => {
      if (!liveRef.current) {
        return;
      }
      liveRef.current = false;
      callIdRef.current = null;
      pendingOfferRef.current = null;
      if (ringTimerRef.current) {
        clearTimeout(ringTimerRef.current);
        ringTimerRef.current = null;
      }
      teardownMedia();

      if (reason) {
        setState((prev) => ({ ...IDLE_STATE, status: 'ended', endedReason: reason, callType: prev.callType }));
        endedTimerRef.current = setTimeout(() => setState(IDLE_STATE), ENDED_LINGER_MS);
      } else {
        setState(IDLE_STATE);
      }
    },
    [teardownMedia]
  );

  const flushPendingCandidates = useCallback(() => {
    const pc = pcRef.current;
    if (!pc) {
      return;
    }
    for (const candidate of pendingCandidatesRef.current) {
      void pc.addIceCandidate(candidate).catch(() => {});
    }
    pendingCandidatesRef.current = [];
  }, []);

  const createPeerConnection = useCallback(() => {
    const pc = new RTCPeerConnection({ iceServers: getIceServers() });

    pc.onicecandidate = (event) => {
      if (event.candidate && callIdRef.current) {
        send('IceCandidate', {
          call_id: callIdRef.current,
          candidate: event.candidate.candidate,
          sdp_mid: event.candidate.sdpMid,
          sdp_mline_index: event.candidate.sdpMLineIndex
        });
      }
    };

    pc.ontrack = (event) => {
      setRemoteStream(event.streams[0] ?? new MediaStream([event.track]));
    };

    pc.onconnectionstatechange = () => {
      if (pc.connectionState === 'failed') {
        // a locally-detected drop: tell the server so the peer is freed and the
        // call is cleared. Server-originated ends (CallHungUp/…) never reach here.
        if (callIdRef.current) {
          send('CallHangup', { call_id: callIdRef.current });
        }
        endCall('Connection lost');
      }
    };

    pcRef.current = pc;
    return pc;
  }, [send, endCall]);

  // getUserMedia wrapped so a denied device surfaces in the UI, never as a throw
  const acquireLocalMedia = useCallback(async (callType: CallType): Promise<MediaStream | null> => {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({
        audio: true,
        video: callType === 'video'
      });
      localStreamRef.current = stream;
      setLocalStream(stream);
      return stream;
    } catch {
      endCall('Microphone / camera unavailable');
      return null;
    }
  }, [endCall]);

  const startCall = useCallback(
    async (peerId: string, callType: CallType) => {
      if (liveRef.current) {
        return;
      }
      if (endedTimerRef.current) {
        clearTimeout(endedTimerRef.current);
      }
      liveRef.current = true;

      const callId = generateCallId();
      callIdRef.current = callId;
      setState({
        status: 'outgoing',
        callId,
        peerId,
        callType,
        isMicMuted: false,
        isCameraOff: false,
        endedReason: null
      });

      const stream = await acquireLocalMedia(callType);
      if (!stream) {
        return;
      }

      const pc = createPeerConnection();
      stream.getTracks().forEach((track) => pc.addTrack(track, stream));

      try {
        const offer = await pc.createOffer();
        await pc.setLocalDescription(offer);
        await invokeRef.current('CallOffer', {
          callee_id: peerId,
          call_id: callId,
          call_type: callType,
          sdp: offer.sdp ?? ''
        });
      } catch {
        // the offer may have registered before the invoke rejected; clear it so a
        // half-sent offer never leaves us "busy" server-side.
        if (callIdRef.current) {
          send('CallHangup', { call_id: callIdRef.current });
        }
        endCall('Could not reach the call service');
      }
    },
    [acquireLocalMedia, createPeerConnection, endCall, send]
  );

  const acceptCall = useCallback(async () => {
    const pending = pendingOfferRef.current;
    if (!pending) {
      return;
    }

    // the ring was answered; stop the auto-dismiss timer so it can't fire mid-call
    if (ringTimerRef.current) {
      clearTimeout(ringTimerRef.current);
      ringTimerRef.current = null;
    }

    const stream = await acquireLocalMedia(pending.call_type);
    if (!stream) {
      send('CallReject', { call_id: pending.call_id });
      return;
    }

    const pc = createPeerConnection();
    stream.getTracks().forEach((track) => pc.addTrack(track, stream));

    try {
      await pc.setRemoteDescription({ type: 'offer', sdp: pending.sdp });
      remoteDescSetRef.current = true;
      flushPendingCandidates();

      const answer = await pc.createAnswer();
      await pc.setLocalDescription(answer);
      await invokeRef.current('CallAnswer', { call_id: pending.call_id, sdp: answer.sdp ?? '' });

      setState((prev) => ({ ...prev, status: 'active', endedReason: null }));
    } catch {
      send('CallHangup', { call_id: pending.call_id });
      endCall('Could not connect the call');
    }
  }, [acquireLocalMedia, createPeerConnection, flushPendingCandidates, send, endCall]);

  const rejectCall = useCallback(() => {
    const pending = pendingOfferRef.current;
    if (pending) {
      send('CallReject', { call_id: pending.call_id });
    }
    endCall(null);
  }, [send, endCall]);

  const hangUp = useCallback(() => {
    if (callIdRef.current) {
      send('CallHangup', { call_id: callIdRef.current });
    }
    endCall(null);
  }, [send, endCall]);

  const toggleMic = useCallback(() => {
    const stream = localStreamRef.current;
    if (!stream) {
      return;
    }
    const nextMuted = stream.getAudioTracks().some((track) => track.enabled);
    stream.getAudioTracks().forEach((track) => {
      track.enabled = !nextMuted;
    });
    setState((prev) => ({ ...prev, isMicMuted: nextMuted }));
  }, []);

  const toggleCamera = useCallback(() => {
    const stream = localStreamRef.current;
    if (!stream) {
      return;
    }
    const nextOff = stream.getVideoTracks().some((track) => track.enabled);
    stream.getVideoTracks().forEach((track) => {
      track.enabled = !nextOff;
    });
    setState((prev) => ({ ...prev, isCameraOff: nextOff }));
  }, []);

  // Register hub event handlers. `on`/`off`/`invoke` change identity when the hub
  // status changes, so this re-runs once the connection object actually exists.
  useEffect(() => {
    const handleIncoming = (payload: IncomingCallEvent) => {
      // ignore ring while already in a call (server also guards busy)
      if (liveRef.current) {
        send('CallReject', { call_id: payload.call_id });
        return;
      }
      if (endedTimerRef.current) {
        clearTimeout(endedTimerRef.current);
      }
      liveRef.current = true;
      callIdRef.current = payload.call_id;
      pendingOfferRef.current = payload;
      pendingCandidatesRef.current = [];
      remoteDescSetRef.current = false;
      setState({
        status: 'incoming',
        callId: payload.call_id,
        peerId: payload.caller_id,
        callType: payload.call_type,
        isMicMuted: false,
        isCameraOff: false,
        endedReason: null
      });

      // dismiss the ring if it is never answered (caller vanished / server timeout)
      ringTimerRef.current = setTimeout(() => {
        if (pendingOfferRef.current) {
          send('CallReject', { call_id: pendingOfferRef.current.call_id });
        }
        endCall(null);
      }, RING_TIMEOUT_MS);
    };

    const handleAnswered = async (payload: CallAnsweredEvent) => {
      const pc = pcRef.current;
      if (!pc || callIdRef.current !== payload.call_id) {
        return;
      }
      try {
        await pc.setRemoteDescription({ type: 'answer', sdp: payload.sdp });
        remoteDescSetRef.current = true;
        flushPendingCandidates();
        setState((prev) => ({ ...prev, status: 'active', endedReason: null }));
      } catch {
        endCall('Could not connect the call');
      }
    };

    const handleRejected = (_payload: CallRejectedEvent) => endCall('Call declined');
    const handleHungUp = (_payload: CallHungUpEvent) => endCall('Call ended');
    const handleFailed = (payload: CallFailedEvent) =>
      endCall(payload.reason === 'user_busy' ? 'User is busy' : 'No answer');

    const handleIceCandidate = (payload: IceCandidatePayload) => {
      if (callIdRef.current !== payload.call_id) {
        return;
      }
      const candidate: RTCIceCandidateInit = {
        candidate: payload.candidate,
        sdpMid: payload.sdp_mid,
        sdpMLineIndex: payload.sdp_mline_index
      };
      const pc = pcRef.current;
      if (pc && remoteDescSetRef.current) {
        void pc.addIceCandidate(candidate).catch(() => {});
      } else {
        pendingCandidatesRef.current.push(candidate);
      }
    };

    on('IncomingCall', handleIncoming as never);
    on('CallAnswered', handleAnswered as never);
    on('CallRejected', handleRejected as never);
    on('CallHungUp', handleHungUp as never);
    on('CallFailed', handleFailed as never);
    on('IceCandidate', handleIceCandidate as never);

    return () => {
      off('IncomingCall', handleIncoming as never);
      off('CallAnswered', handleAnswered as never);
      off('CallRejected', handleRejected as never);
      off('CallHungUp', handleHungUp as never);
      off('CallFailed', handleFailed as never);
      off('IceCandidate', handleIceCandidate as never);
    };
  }, [on, off, send, endCall, flushPendingCandidates]);

  useEffect(() => {
    return () => {
      if (endedTimerRef.current) {
        clearTimeout(endedTimerRef.current);
      }
      if (ringTimerRef.current) {
        clearTimeout(ringTimerRef.current);
      }
      teardownMedia();
    };
  }, [teardownMedia]);

  const value = useMemo<CallContextValue>(
    () => ({
      state,
      localStream,
      remoteStream,
      startCall,
      acceptCall,
      rejectCall,
      hangUp,
      toggleMic,
      toggleCamera
    }),
    [state, localStream, remoteStream, startCall, acceptCall, rejectCall, hangUp, toggleMic, toggleCamera]
  );

  return <CallContext.Provider value={value}>{children}</CallContext.Provider>;
}

export function useCall(): CallContextValue {
  const context = useContext(CallContext);
  if (!context) {
    throw new Error('useCall must be used within a CallProvider');
  }
  return context;
}
