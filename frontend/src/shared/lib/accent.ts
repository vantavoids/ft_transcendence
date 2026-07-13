import type { ChatMessageData } from '../../components/chat-message';
import { hashToIndex } from './hash';

// single source of truth for user/message accent colors - keeps a given
// user's color consistent across the profile card, guild member list, and
// chat messages
const ACCENTS: ChatMessageData['accent'][] = ['aqua', 'yellow', 'lime', 'lavender', 'pink'];

export function accentForId(id: string): ChatMessageData['accent'] {
  return ACCENTS[hashToIndex(id, ACCENTS.length)];
}
