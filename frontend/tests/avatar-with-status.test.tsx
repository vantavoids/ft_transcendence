import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { renderToStaticMarkup } from 'react-dom/server';
import { AvatarWithStatus } from '../src/components/avatar-with-status';

describe('AvatarWithStatus', () => {
  it('renders a real image when an avatar URL is provided', () => {
    const html = renderToStaticMarkup(
      <AvatarWithStatus
        name="SkyDogzz"
        accent="aqua"
        status="online"
        avatarUrl="https://cdn.example/avatar.png"
      />
    );

    assert.ok(html.includes('<img'));
    assert.ok(html.includes('src="https://cdn.example/avatar.png"'));
  });

  it('falls back to an initial bubble when there is no avatar URL', () => {
    const html = renderToStaticMarkup(
      <AvatarWithStatus name="SkyDogzz" accent="aqua" status="online" avatarUrl={null} />
    );

    assert.ok(!html.includes('<img'));
    assert.ok(html.includes('S'));
  });
});
