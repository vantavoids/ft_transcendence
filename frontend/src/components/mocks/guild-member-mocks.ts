import type { GuildMember } from '../guild-member-list';

// TODO(api:guild,user): load GET /guilds/{id}/members, then hydrate each user via GET /users/{id}.
export const guildMembers: GuildMember[] = [
  {
    id: 'user-alpha',
    name: 'User Alpha',
    role: 'Owner',
    status: 'online',
    accent: 'yellow',
    activity: 'In #general'
  },
  {
    id: 'user-gamma',
    name: 'User Gamma',
    role: 'Admin',
    status: 'idle',
    accent: 'lime',
    activity: 'Watching ladder'
  },
  {
    id: 'user-beta',
    name: 'User Beta',
    role: 'Member',
    status: 'online',
    accent: 'aqua',
    activity: 'Queue ready'
  },
  {
    id: 'user-epsilon',
    name: 'User Epsilon',
    role: 'Member',
    status: 'online',
    accent: 'pink',
    activity: 'Writing'
  },
  {
    id: 'user-delta',
    name: 'User Delta',
    role: 'Member',
    status: 'offline',
    accent: 'lavender',
    activity: 'Offline'
  }
];
