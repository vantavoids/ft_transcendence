import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { renderToStaticMarkup } from 'react-dom/server';
import { ProfileEditorPanel } from '../src/components/profile-editor-panel';
import type { CurrentUserProfile } from '../src/shared/mappers/user';

const currentUser: CurrentUserProfile = {
  id: '123',
  username: 'skydogzz',
  displayName: 'SkyDogzz',
  avatarUrl: 'https://cdn.example/current-avatar.png',
  bannerUrl: 'https://cdn.example/current-banner.png',
  status: 'online',
  bio: 'Terminal first.',
  lastSeenAt: '2026-07-11T00:00:00Z',
  lastSeenLabel: '11 Jul 00:00'
};

describe('SettingsModal profile panel', () => {
  it('renders avatar and banner controls for profile edits', () => {
    const html = renderToStaticMarkup(
      <ProfileEditorPanel
        currentUser={currentUser}
        onBack={() => undefined}
        onSaveProfile={async () => undefined}
        onUploadAvatar={async () => undefined}
        onRemoveAvatar={async () => undefined}
        onUploadBanner={async () => undefined}
        onRemoveBanner={async () => undefined}
      />
    );

    assert.ok(html.includes('Avatar'));
    assert.ok(html.includes('Banner'));
    assert.ok(html.includes('Remove avatar'));
    assert.ok(html.includes('Remove banner'));
    assert.ok(html.includes('https://cdn.example/current-avatar.png'));
    assert.ok(html.includes('https://cdn.example/current-banner.png'));
  });
});
