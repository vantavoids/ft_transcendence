import { useEffect, useRef } from 'react';

// Bind a MediaStream to a <video>/<audio> element and (re)start playback,
// swallowing the autoplay-policy promise rejection so Chrome's console stays clean.
export function useMediaStream<T extends HTMLMediaElement>(stream: MediaStream | null) {
  const ref = useRef<T>(null);

  useEffect(() => {
    const element = ref.current;
    if (!element) {
      return;
    }

    if (element.srcObject !== stream) {
      element.srcObject = stream;
    }

    if (stream) {
      void element.play().catch(() => {
        /* autoplay blocked until user gesture; controls remain usable */
      });
    }
  }, [stream]);

  return ref;
}
