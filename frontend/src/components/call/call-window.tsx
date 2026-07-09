'use client';

import { useEffect, useState } from 'react';
import { Maximize2, Mic, MicOff, Minimize2, PhoneOff, Video, VideoOff } from 'lucide-react';
import { useCall } from '../../shared/call/call-context';
import { useMediaStream } from './use-media-stream';
import { useSpeaking } from './use-speaking';

// glowing aqua ring shown around whoever is currently talking
const SPEAKING_RING = 'ring-2 ring-aqua shadow-[0_0_26px_rgba(120,220,232,0.55)]';
// inset variant for the minimized card's video area (its glow would otherwise be
// clipped by the card's overflow-hidden, and we only want the upper part ringed)
const SPEAKING_RING_INSET = 'ring-2 ring-inset ring-aqua shadow-[inset_0_0_20px_rgba(120,220,232,0.4)]';

type CallTileProps = {
  stream: MediaStream | null;
  label: string;
  // this tile is the local user: mirror the video (audio is handled elsewhere)
  self: boolean;
  isVideo: boolean;
  cameraOff: boolean;
  // whether this participant is currently talking (detected in CallWindow)
  speaking: boolean;
  // peer has been called but has not answered yet
  ringing: boolean;
  compact?: boolean;
};

function CallTile({
  stream,
  label,
  self,
  isVideo,
  cameraOff,
  speaking,
  ringing,
  compact = false
}: CallTileProps) {
  const videoRef = useMediaStream<HTMLVideoElement>(stream);
  const showVideo = isVideo && !cameraOff && !!stream;

  const tileRing = speaking
    ? SPEAKING_RING
    : ringing
      ? 'ring-1 ring-white/10 opacity-80'
      : 'ring-1 ring-white/10';

  const avatarSize = compact ? 'h-14 w-14 text-xl' : 'h-24 w-24 text-3xl';

  return (
    <div
      className={`relative flex h-full w-full min-h-0 items-center justify-center overflow-hidden rounded-[1rem] bg-panel transition-shadow ${tileRing}`}
    >
      {/* videos are muted; audio plays through the persistent <audio> in CallWindow */}
      <video
        ref={videoRef}
        autoPlay
        playsInline
        muted
        className={`h-full w-full object-cover ${showVideo ? '' : 'invisible'} ${self ? '-scale-x-100' : ''}`}
      />

      {!showVideo ? (
        <div className="absolute inset-0 flex flex-col items-center justify-center gap-3">
          <span
            className={`relative flex items-center justify-center rounded-full bg-aqua/10 font-bold text-aqua ring-1 ring-aqua/30 ${avatarSize}`}
          >
            {ringing ? (
              <span className="absolute inset-0 animate-ping rounded-full ring-2 ring-aqua/50" aria-hidden />
            ) : null}
            {label.slice(0, 1).toUpperCase()}
          </span>
          {ringing && !compact ? (
            <span className="font-category animate-pulse text-[0.7rem] uppercase tracking-[0.18em] text-aqua/70">
              Ringing…
            </span>
          ) : null}
        </div>
      ) : null}

      {!compact ? (
        <span className="absolute bottom-3 left-3 rounded-md bg-black/55 px-2.5 py-1 text-xs font-bold tracking-[-0.01em] text-white">
          {label}
        </span>
      ) : null}
    </div>
  );
}

type CallWindowProps = {
  resolvePeerName?: (peerId: string | null) => string;
};

export function CallWindow({ resolvePeerName }: CallWindowProps) {
  const { state, localStream, remoteStream, hangUp, toggleMic, toggleCamera } = useCall();
  const [minimized, setMinimized] = useState(false);
  // remote audio lives here so it keeps playing across the minimize/maximize swap
  const remoteAudioRef = useMediaStream<HTMLAudioElement>(remoteStream);
  // detect talkers here (not in CallTile) so analysis survives minimize/maximize
  // instead of tearing down and rebuilding an AudioContext on every remount
  const remoteSpeaking = useSpeaking(remoteStream);
  const localSpeaking = useSpeaking(localStream, !state.isMicMuted);

  // every fresh call should open maximized
  useEffect(() => {
    if (state.status === 'idle') {
      setMinimized(false);
    }
  }, [state.status]);

  const isVisible =
    state.status === 'outgoing' || state.status === 'active' || state.status === 'ended';

  if (!isVisible) {
    return null;
  }

  const peerName = resolvePeerName?.(state.peerId) ?? 'Unknown user';
  const isVideo = state.callType === 'video';
  const ringing = state.status === 'outgoing';

  const statusLabel = ringing
    ? 'Ringing…'
    : state.status === 'ended'
      ? (state.endedReason ?? 'Call ended')
      : 'Connected';

  const micButton = (size: 'sm' | 'lg') => (
    <button
      type="button"
      onClick={toggleMic}
      className={`flex items-center justify-center rounded-full ring-1 transition ${
        size === 'lg' ? 'h-14 w-14' : 'h-9 w-9'
      } ${
        state.isMicMuted
          ? 'bg-pink/15 text-pink ring-pink/40 hover:bg-pink/25'
          : 'bg-white/10 text-white ring-white/15 hover:bg-white/20'
      }`}
      aria-label={state.isMicMuted ? 'Unmute microphone' : 'Mute microphone'}
      aria-pressed={state.isMicMuted}
    >
      {state.isMicMuted ? (
        <MicOff className={size === 'lg' ? 'h-6 w-6' : 'h-4 w-4'} strokeWidth={2} />
      ) : (
        <Mic className={size === 'lg' ? 'h-6 w-6' : 'h-4 w-4'} strokeWidth={2} />
      )}
    </button>
  );

  const hangupButton = (size: 'sm' | 'lg') => (
    <button
      type="button"
      onClick={hangUp}
      className={`flex items-center justify-center rounded-full bg-pink text-black ring-1 ring-pink/60 transition hover:bg-pink/90 ${
        size === 'lg' ? 'h-14 w-14' : 'h-9 w-9'
      }`}
      aria-label="Hang up"
    >
      <PhoneOff className={size === 'lg' ? 'h-6 w-6' : 'h-4 w-4'} strokeWidth={2} />
    </button>
  );

  return (
    <>
      {/* keeps remote audio audible whether the call is minimized or full-screen */}
      <audio ref={remoteAudioRef} autoPlay className="hidden" />

      {minimized ? (
        <div className="fixed bottom-4 right-4 z-[55] w-72 overflow-hidden rounded-[1rem] bg-secondary-bg shadow-2xl shadow-black/50 ring-1 ring-white/10">
          <button
            type="button"
            onClick={() => setMinimized(false)}
            className={`group relative block aspect-video w-full transition-shadow ${
              remoteSpeaking ? SPEAKING_RING_INSET : ''
            }`}
            aria-label="Expand call"
          >
            <CallTile
              stream={remoteStream}
              label={peerName}
              self={false}
              isVideo={isVideo}
              cameraOff={false}
              speaking={false}
              ringing={ringing}
              compact
            />
            <span className="absolute right-2 top-2 flex h-7 w-7 items-center justify-center rounded-md bg-black/50 text-white/80 transition group-hover:bg-black/70 group-hover:text-white">
              <Maximize2 className="h-4 w-4" strokeWidth={2} />
            </span>
          </button>

          <div className="flex items-center gap-2 px-3 py-2">
            <div className="min-w-0 flex-1">
              <p className="truncate text-sm font-bold text-white">{peerName}</p>
              <p className="font-category text-[0.62rem] uppercase tracking-[0.14em] text-white/45">
                {statusLabel}
              </p>
            </div>
            {micButton('sm')}
            {hangupButton('sm')}
          </div>
        </div>
      ) : (
        <div className="fixed inset-0 z-[55] flex flex-col bg-black/85 backdrop-blur-sm">
          <button
            type="button"
            onClick={() => setMinimized(true)}
            className="absolute right-5 top-5 z-10 flex h-10 w-10 items-center justify-center rounded-full bg-white/10 text-white ring-1 ring-white/15 transition hover:bg-white/20"
            aria-label="Minimize call"
          >
            <Minimize2 className="h-5 w-5" strokeWidth={2} />
          </button>

          <div className="shrink-0 pt-6 text-center">
            <h2 className="text-[1.35rem] font-bold tracking-[-0.03em] text-white">{peerName}</h2>
            <p className="font-category mt-1 text-[0.72rem] uppercase tracking-[0.16em] text-white/50">
              {statusLabel}
            </p>
          </div>

          <div className="grid min-h-0 flex-1 grid-cols-1 gap-4 p-4 sm:grid-cols-2 md:p-6">
            <CallTile
              stream={remoteStream}
              label={peerName}
              self={false}
              isVideo={isVideo}
              cameraOff={false}
              speaking={remoteSpeaking}
              ringing={ringing}
            />
            <CallTile
              stream={localStream}
              label="You"
              self
              isVideo={isVideo}
              cameraOff={state.isCameraOff}
              speaking={localSpeaking}
              ringing={false}
            />
          </div>

          <div className="flex shrink-0 items-center justify-center gap-4 py-8">
            {micButton('lg')}

            {isVideo ? (
              <button
                type="button"
                onClick={toggleCamera}
                className={`flex h-14 w-14 items-center justify-center rounded-full ring-1 transition ${
                  state.isCameraOff
                    ? 'bg-pink/15 text-pink ring-pink/40 hover:bg-pink/25'
                    : 'bg-white/10 text-white ring-white/15 hover:bg-white/20'
                }`}
                aria-label={state.isCameraOff ? 'Turn camera on' : 'Turn camera off'}
                aria-pressed={state.isCameraOff}
              >
                {state.isCameraOff ? (
                  <VideoOff className="h-6 w-6" strokeWidth={2} />
                ) : (
                  <Video className="h-6 w-6" strokeWidth={2} />
                )}
              </button>
            ) : null}

            {hangupButton('lg')}
          </div>
        </div>
      )}
    </>
  );
}
