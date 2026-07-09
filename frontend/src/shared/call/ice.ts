// ICE servers for the peer connection, built strictly from env. coturn answers
// both STUN and TURN, so a single configured host yields both entries. There is
// no public STUN fallback on purpose: the 42 eval box runs coturn loopback-only.

let warnedMissing = false;

export function getIceServers(): RTCIceServer[] {
  const url = process.env.NEXT_PUBLIC_TURN_URL?.trim();
  const username = process.env.NEXT_PUBLIC_TURN_USERNAME?.trim();
  const credential = process.env.NEXT_PUBLIC_TURN_PASSWORD?.trim();

  if (!url) {
    if (!warnedMissing && process.env.NODE_ENV !== 'production') {
      warnedMissing = true;
      console.warn(
        'NEXT_PUBLIC_TURN_URL is unset; calls will rely on host-local candidates only.'
      );
    }
    return [];
  }

  // strip any turn:/turns:/stun: scheme so we can derive both from the bare host
  const host = url.replace(/^(turns?|stun):/i, '');
  const isSecure = /^turns:/i.test(url);
  const turnScheme = isSecure ? 'turns' : 'turn';

  const servers: RTCIceServer[] = [{ urls: `stun:${host}` }];

  if (username && credential) {
    servers.push({ urls: `${turnScheme}:${host}`, username, credential });
  }

  return servers;
}
