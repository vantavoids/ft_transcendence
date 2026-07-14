import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { renderToStaticMarkup } from 'react-dom/server';
import { ChatMessage, type ChatMessageData } from '../src/components/chat-message';

const message: ChatMessageData = {
  id: 'm-1',
  authorId: 'u-1',
  author: 'BlockedUser',
  accent: 'pink',
  content: ['This should stay hidden first.'],
  timestamp: '10:30'
};

describe('ChatMessage', () => {
  it('collapses blocked guild messages until the viewer reveals them', () => {
    const html = renderToStaticMarkup(
      <ChatMessage
        message={message}
        isGrouped={false}
        isOwnMessage={false}
        isEditing={false}
        isBlockedMessage
        editingDraft=""
        canReact={false}
        onEditDraftChange={() => undefined}
        onStartEdit={() => undefined}
        onSaveEdit={() => undefined}
        onCancelEdit={() => undefined}
        onDelete={() => undefined}
        onToggleReaction={() => undefined}
        onReply={() => undefined}
        onJumpToReply={() => undefined}
        onRetryMessage={() => undefined}
        setMessageRef={() => undefined}
      />
    );

    assert.ok(html.includes('Blocked message'));
    assert.ok(!html.includes('This should stay hidden first.'));
  });
});
