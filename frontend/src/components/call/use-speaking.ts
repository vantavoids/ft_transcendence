import { useEffect, useRef, useState } from 'react';

// RMS above this (0..1) counts as voice; keep "speaking" true for a short hangover
// afterwards so brief pauses between words don't make the indicator flicker.
const SPEAKING_RMS_THRESHOLD = 0.02;
const HANGOVER_MS = 300;

// Detect whether the audio in `stream` is currently active ("speaking"), via a
// Web Audio AnalyserNode. `enabled` lets the caller force it off (e.g. muted mic).
// The analyser is never connected to the destination, so it adds no audible output;
// the remote stream is already audible through its <audio> element.
//
// In a video call the remote audio and video tracks arrive as separate `ontrack`
// events, so the audio track may be added to the (already-set) stream after this
// hook first runs. We therefore initialise on the stream's `addtrack` event too,
// not just once, so late-arriving audio still gets analysed.
export function useSpeaking(stream: MediaStream | null, enabled = true): boolean {
  const [speaking, setSpeaking] = useState(false);
  const speakingRef = useRef(false);

  useEffect(() => {
    speakingRef.current = false;
    setSpeaking(false);

    if (!stream || !enabled) {
      return;
    }

    const AudioCtx =
      window.AudioContext ??
      (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
    if (!AudioCtx) {
      return;
    }

    let context: AudioContext | null = null;
    let source: MediaStreamAudioSourceNode | null = null;
    let frame = 0;
    let lastAbove = 0;

    const start = () => {
      if (context || stream.getAudioTracks().length === 0) {
        return;
      }

      context = new AudioCtx();
      source = context.createMediaStreamSource(stream);
      const analyser = context.createAnalyser();
      analyser.fftSize = 512;
      analyser.smoothingTimeConstant = 0.2;
      source.connect(analyser);

      const samples = new Uint8Array(analyser.fftSize);
      const tick = () => {
        analyser.getByteTimeDomainData(samples);
        let sum = 0;
        for (let i = 0; i < samples.length; i += 1) {
          const centered = (samples[i] - 128) / 128;
          sum += centered * centered;
        }
        const rms = Math.sqrt(sum / samples.length);
        const now = performance.now();
        if (rms > SPEAKING_RMS_THRESHOLD) {
          lastAbove = now;
        }

        const next = now - lastAbove < HANGOVER_MS;
        if (next !== speakingRef.current) {
          speakingRef.current = next;
          setSpeaking(next);
        }
        frame = requestAnimationFrame(tick);
      };

      // resume in case the browser created the context suspended (autoplay policy)
      void context.resume().catch(() => {});
      frame = requestAnimationFrame(tick);
    };

    start();
    // (re)initialise if the audio track arrives after the stream was set
    stream.addEventListener('addtrack', start);

    return () => {
      stream.removeEventListener('addtrack', start);
      cancelAnimationFrame(frame);
      source?.disconnect();
      void context?.close().catch(() => {});
      speakingRef.current = false;
      setSpeaking(false);
    };
  }, [stream, enabled]);

  return speaking;
}
