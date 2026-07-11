'use client';

import { Phone, PhoneOff, Video } from 'lucide-react';
import { useCall } from '../../shared/call/call-context';

type IncomingCallOverlayProps = {
  // resolve a peer user id to a display name (mock DMs / guild members for now)
  resolvePeerName?: (peerId: string | null) => string;
};

export function IncomingCallOverlay({ resolvePeerName }: IncomingCallOverlayProps) {
  const { state, acceptCall, rejectCall } = useCall();

  if (state.status !== 'incoming') {
    return null;
  }

  const peerName = resolvePeerName?.(state.peerId) ?? 'Unknown user';
  const isVideo = state.callType === 'video';

  return (
    <div className="fixed inset-0 z-[60] flex items-center justify-center bg-black/60 px-4 py-6">
      <section className="relative w-full max-w-[24rem] overflow-hidden rounded-[1rem] bg-secondary-bg p-6 text-center shadow-2xl shadow-black/50 ring-1 ring-stroke">
        <span className="mx-auto flex h-20 w-20 items-center justify-center rounded-full bg-aqua/10 text-2xl font-bold text-aqua ring-1 ring-aqua/30">
          {peerName.slice(0, 1).toUpperCase()}
        </span>
        <h2 className="mt-4 truncate text-[1.35rem] font-bold tracking-[-0.03em] text-white">
          {peerName}
        </h2>
        <p className="font-category mt-1 flex items-center justify-center gap-2 text-[0.72rem] uppercase tracking-[0.14em] text-white/40">
          <span className="inline-flex h-4 w-4 items-center justify-center">
            {isVideo ? <Video className="h-4 w-4" strokeWidth={1.9} /> : <Phone className="h-4 w-4" strokeWidth={1.9} />}
          </span>
          Incoming {isVideo ? 'video' : 'voice'} call
        </p>

        <div className="mt-7 flex items-center justify-center gap-4">
          <button
            type="button"
            onClick={rejectCall}
            className="flex h-14 w-14 items-center justify-center rounded-full bg-pink/15 text-pink ring-1 ring-pink/40 transition hover:bg-pink/25"
            aria-label="Decline call"
          >
            <PhoneOff className="h-6 w-6" strokeWidth={2} />
          </button>
          <button
            type="button"
            onClick={() => void acceptCall()}
            className="flex h-14 w-14 items-center justify-center rounded-full bg-lime/15 text-lime ring-1 ring-lime/40 transition hover:bg-lime/25"
            aria-label="Accept call"
          >
            <Phone className="h-6 w-6" strokeWidth={2} />
          </button>
        </div>
      </section>
    </div>
  );
}
