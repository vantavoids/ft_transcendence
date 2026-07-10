import { fetchAuthedResource } from '../api/client';

// A browser navigation (<a href>, window.open, or a download) can't carry the
// Authorization header, so authenticated attachment URLs 401 when opened or
// downloaded directly. These helpers fetch the bytes through the authed client
// (Bearer + single-flight refresh) and hand the browser a temporary blob URL
// instead. The object URL is revoked on a delay so the new tab / download has
// time to consume it before it's freed.

const REVOKE_DELAY_MS = 60_000;

async function fetchAsObjectUrl(url: string): Promise<string> {
  const res = await fetchAuthedResource(url);
  if (!res.ok) {
    throw new Error(`attachment request failed (${res.status})`);
  }
  const blob = await res.blob();
  return URL.createObjectURL(blob);
}

function revokeLater(objectUrl: string): void {
  setTimeout(() => URL.revokeObjectURL(objectUrl), REVOKE_DELAY_MS);
}

// Opens an authenticated attachment in a new tab. The blank tab is opened
// synchronously inside the click gesture so the popup blocker allows it, then
// pointed at the blob URL once the authed fetch resolves.
export async function openAuthedAttachment(url: string): Promise<void> {
  const tab = window.open('about:blank', '_blank');
  try {
    const objectUrl = await fetchAsObjectUrl(url);
    if (tab) {
      tab.location.href = objectUrl;
    }
    revokeLater(objectUrl);
  } catch (err) {
    tab?.close();
    throw err;
  }
}

// Saves an authenticated attachment to disk under its original filename.
export async function downloadAuthedAttachment(url: string, filename: string): Promise<void> {
  const objectUrl = await fetchAsObjectUrl(url);
  const anchor = document.createElement('a');
  anchor.href = objectUrl;
  anchor.download = filename;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  revokeLater(objectUrl);
}
