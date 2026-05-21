import type { GuildMember } from '../guild-member-list';

// TODO(api:guild,user): load GET /guilds/{id}/members, then hydrate each user via GET /users/{id}.
export const guildMembers: GuildMember[] = [
  {
    id: 'skydogzz',
    name: 'SkyDogzz',
    role: 'Owner',
    status: 'online',
    accent: 'yellow',
    activity: 'In #general'
  },
  {
    id: 'um4ss',
    name: 'um4ss',
    role: 'Admin',
    status: 'idle',
    accent: 'lime',
    activity: 'Watching ladder'
  },
  {
    id: 'add',
    name: 'add',
    role: 'Member',
    status: 'online',
    accent: 'aqua',
    activity: 'Queue ready'
  },
  {
    id: 'cartoone',
    name: 'Cartoone',
    role: 'Member',
    status: 'online',
    accent: 'pink',
    activity: 'Writing'
  },
  {
    id: 'vanta',
    name: 'Vanta',
    role: 'Member',
    status: 'offline',
    accent: 'lavender',
    activity: 'Offline'
  }
];
