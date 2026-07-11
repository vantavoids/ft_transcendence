import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { renderToStaticMarkup } from 'react-dom/server';
import { ChannelList, type ChannelCategory } from '../src/components/channel-list';
import { GuildProvider } from '../src/shared/guilds/guild-store';

const categories: ChannelCategory[] = [
  {
    id: 'cat-1',
    name: 'General',
    channels: [{ id: 'chan-1', name: 'general' }]
  }
];

describe('ChannelList', () => {
  it('renders a guild settings entry in chat mode', () => {
    const html = renderToStaticMarkup(
      <GuildProvider>
        <ChannelList
          activeChannel="chan-1"
          categories={categories}
          unreadCounts={{}}
          mobilePane="channels"
          currentUser={null}
          isMicMuted={false}
          isDeafened={false}
          unreadNotifications={0}
          onToggleDeafen={() => undefined}
          onToggleMicMute={() => undefined}
          onOpenNotifications={() => undefined}
          onOpenSettings={() => undefined}
          onOpenGuildSettings={() => undefined}
          onSelectChannel={() => undefined}
        />
      </GuildProvider>
    );

    assert.ok(html.includes('Open guild settings'));
  });
});
