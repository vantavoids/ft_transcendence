// deterministic (not random) index into a fixed-length list, so the same id
// always picks the same slot (accent color, etc.) - shared math, not shared
// meaning: callers keep their own list and return shape
export function hashToIndex(value: string, length: number): number {
  let hash = 0;
  for (let index = 0; index < value.length; index += 1) {
    hash = (hash * 31 + value.charCodeAt(index)) | 0;
  }

  return Math.abs(hash) % length;
}
