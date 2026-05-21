import type { ChannelCategory } from '../channel-list';

// TODO(api:guild): replace local categories with GET /guilds/{id}/channels and category endpoints.
export const channelCategories: ChannelCategory[] = [
  {
    id: 'welcome',
    name: 'Start Here',
    channels: [
      { id: 'general', name: 'general' },
      { id: 'rules', name: 'rules' },
      { id: 'announcements', name: 'announcements' },
      { id: 'introductions', name: 'introductions' }
    ]
  },
  {
    id: 'transcendence',
    name: 'Transcendence',
    channels: [
      { id: 'matchmaking', name: 'matchmaking' },
      { id: 'tournaments', name: 'tournaments' },
      { id: 'ranked-ladder', name: 'ranked-ladder' },
      { id: 'replays', name: 'replays' },
      { id: 'patch-notes', name: 'patch-notes' }
    ]
  },
  {
    id: 'dev',
    name: 'Dev',
    channels: [
      { id: 'frontend', name: 'frontend' },
      { id: 'backend', name: 'backend' },
      { id: 'infra', name: 'infra' },
      { id: 'api-contracts', name: 'api-contracts' },
      { id: 'bugs', name: 'bugs' },
      { id: 'ideas_are_tough', name: 'ideas_are_tough' }
    ]
  },
  {
    id: 'community',
    name: 'Community',
    channels: [
      { id: 'idk', name: 'idk' },
      { id: 'clips', name: 'clips' },
      { id: 'screenshots', name: 'screenshots' },
      { id: 'music', name: 'music' },
      { id: 'off-topic', name: 'off-topic' }
    ]
  },
  {
    id: 'teams',
    name: 'Teams',
    channels: [
      { id: 'team-alpha', name: 'team-alpha' },
      { id: 'team-beta', name: 'team-beta' },
      { id: 'team-gamma', name: 'team-gamma' },
      { id: 'team-delta', name: 'team-delta' }
    ]
  },
  {
    id: 'archive',
    name: 'Archive',
    channels: [
      { id: 'old-general', name: 'old-general' },
      { id: 'season-one', name: 'season-one' },
      { id: 'season-two', name: 'season-two' },
      { id: 'random-notes', name: 'random-notes' }
    ]
  }
];
