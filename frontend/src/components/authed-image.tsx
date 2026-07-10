'use client';

import { useEffect, useState } from 'react';

import { fetchAuthedResource } from '../shared/api/client';

type AuthedImageProps = {
  src: string;
  alt: string;
  className?: string;
};

// A browser cannot attach an Authorization header to a plain <img src>, so an
// authenticated attachment URL 401s when rendered directly. Fetch it through the
// authed client (Bearer + single-flight refresh), turn the response into a blob
// URL, and render that; revoke the blob URL on unmount / src change so we don't
// leak object URLs.
export function AuthedImage({ src, alt, className }: AuthedImageProps) {
  const [objectUrl, setObjectUrl] = useState<string | null>(null);
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    let active = true;
    let created: string | null = null;
    setObjectUrl(null);
    setFailed(false);

    void (async () => {
      try {
        const res = await fetchAuthedResource(src);
        if (!res.ok) {
          if (active) setFailed(true);
          return;
        }
        const blob = await res.blob();
        if (!active) return;
        created = URL.createObjectURL(blob);
        setObjectUrl(created);
      } catch {
        if (active) setFailed(true);
      }
    })();

    return () => {
      active = false;
      if (created) URL.revokeObjectURL(created);
    };
  }, [src]);

  if (failed) {
    return (
      <div
        className={`flex items-center justify-center bg-panel text-xs text-white/40 ${className ?? ''}`}
      >
        Failed to load image
      </div>
    );
  }

  if (!objectUrl) {
    return <div className={`animate-pulse bg-white/5 ${className ?? ''}`} aria-busy="true" />;
  }

  // eslint-disable-next-line @next/next/no-img-element -- blob: object URL for an authed attachment, nothing next/image can optimize
  return <img src={objectUrl} alt={alt} className={className} />;
}
