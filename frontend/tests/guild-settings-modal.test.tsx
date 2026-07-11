import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { renderToStaticMarkup } from 'react-dom/server';
import { GuildProvider } from '../src/shared/guilds/guild-store';
import { GuildSettingsModal } from '../src/components/guild/guild-settings-modal';

describe('GuildSettingsModal', () => {
  it('renders the guild management menu inside the modal', () => {
    const html = renderToStaticMarkup(
      <GuildProvider>
        <GuildSettingsModal guildId="guild-123" onClose={() => undefined} />
      </GuildProvider>
    );

    assert.ok(html.includes('Guild management'));
    assert.ok(html.includes('Overview'));
    assert.ok(html.includes('Members'));
    assert.ok(html.includes('Bans'));
    assert.ok(html.includes('Invites'));
    assert.ok(html.includes('Roles'));
    assert.ok(html.includes('Categories'));
    assert.ok(html.includes('Channels'));
    assert.ok(html.includes('Settings'));
    assert.ok(html.includes('Create guild'));
    assert.ok(html.includes('Join guild'));
  });
});
