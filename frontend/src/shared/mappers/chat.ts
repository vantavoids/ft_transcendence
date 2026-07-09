import type { ChatMessageData } from '../../components/chat-message';

type MessageAccent = ChatMessageData['accent'];

const MESSAGE_ACCENTS: MessageAccent[] = ['aqua', 'yellow', 'lime', 'lavender', 'pink'];

export function formatMessageTimestamp(isoTimestamp: string): string {
  return new Date(isoTimestamp).toLocaleTimeString('fr-FR', {
    hour: '2-digit',
    minute: '2-digit'
  });
}

export function splitMessageLines(content: string): string[] {
  return content.split(/\r?\n/);
}

// deterministic (not random) so the same author always renders with the same accent color
export function accentForAuthor(authorId: string): MessageAccent {
  let hash = 0;

  for (let index = 0; index < authorId.length; index += 1) {
    hash = (hash * 31 + authorId.charCodeAt(index)) | 0;
  }

  return MESSAGE_ACCENTS[Math.abs(hash) % MESSAGE_ACCENTS.length];
}
