import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { mapChannelMessage, mapDirectMessage, mapDirectMessageConversation } from '../src/shared/mappers/chat';
import type { ChannelMessageDto, DirectMessageConversationDto, DirectMessageDto } from '../src/shared/api/chat';
import type { UserSummaryDto } from '../src/shared/api/user';

const usersById: Record<string, UserSummaryDto> = {
  '123': {
    id: '123',
    username: 'tstephan',
    display_name: 'TStephan',
    avatar_url: 'https://cdn.example/avatar.png',
    banner_url: 'https://cdn.example/banner.png',
    status: 'online',
    bio: 'Shipping.'
  }
};

describe('chat mappers', () => {
  it('hydrates channel message authors from user profiles', () => {
    const dto: ChannelMessageDto = {
      id: 'm1',
      channel_id: 'c1',
      author_id: '123',
      content: 'hello',
      reply_to_id: null,
      edited_at: null,
      created_at: '2026-07-11T00:00:00Z',
      attachments: [],
      reactions: []
    };

    const mapped = mapChannelMessage(dto, null, usersById);

    assert.equal(mapped.author, 'TStephan');
    assert.equal(mapped.avatarUrl, 'https://cdn.example/avatar.png');
  });

  it('leaves avatarUrl null when the author profile is not loaded', () => {
    const dto: ChannelMessageDto = {
      id: 'm3',
      channel_id: 'c1',
      author_id: 'unknown',
      content: 'hello',
      reply_to_id: null,
      edited_at: null,
      created_at: '2026-07-11T00:00:00Z',
      attachments: [],
      reactions: []
    };

    assert.equal(mapChannelMessage(dto, null, usersById).avatarUrl, null);
  });

  it('hydrates direct message authors and conversations from user profiles', () => {
    const message: DirectMessageDto = {
      id: 'm2',
      sender_id: '123',
      recipient_id: '999',
      content: 'hi',
      reply_to_id: null,
      created_at: '2026-07-11T00:00:00Z',
      edited_at: null,
      attachments: []
    };
    const conversation: DirectMessageConversationDto = {
      partner_id: '123',
      last_preview: 'hi',
      last_message_at: '2026-07-11T00:00:00Z',
      unread_count: 0,
      is_archived: false
    };

    assert.equal(mapDirectMessage(message, null, usersById).author, 'TStephan');
    assert.equal(mapDirectMessage(message, null, usersById).avatarUrl, 'https://cdn.example/avatar.png');
    assert.equal(mapDirectMessageConversation(conversation, usersById).name, 'TStephan');
  });

  it('carries a DM edited_at through so the "edited" label survives a refresh', () => {
    const edited: DirectMessageDto = {
      id: 'm4',
      sender_id: '123',
      recipient_id: '999',
      content: 'fixed typo',
      reply_to_id: null,
      created_at: '2026-07-11T00:00:00Z',
      edited_at: '2026-07-11T00:05:00Z',
      attachments: []
    };

    assert.equal(mapDirectMessage(edited, null, usersById).editedAt, '2026-07-11T00:05:00Z');
  });
});
