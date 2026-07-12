// Shared mute-duration options so the notification card and the guild context
// menu offer the same choices and compute muted_until identically.

// `label` stays compact for the notification card's 4-up button row; `longLabel`
// reads naturally in the guild context submenu's vertical list.
export const muteDurations = [
  { value: '1h', label: '1h', longLabel: '1 hour', hours: 1 },
  { value: '8h', label: '8h', longLabel: '8 hours', hours: 8 },
  { value: '24h', label: '24h', longLabel: '1 day', hours: 24 },
  { value: 'forever', label: 'Forever', longLabel: 'Forever', hours: null }
] as const;

export type MuteDuration = (typeof muteDurations)[number]['value'];

// null means an indefinite mute (no expiry); otherwise an ISO timestamp `hours`
// from now.
export function muteDurationToIso(duration: MuteDuration): string | null {
  const hours = muteDurations.find((option) => option.value === duration)?.hours ?? null;
  return hours === null ? null : new Date(Date.now() + hours * 3_600_000).toISOString();
}
