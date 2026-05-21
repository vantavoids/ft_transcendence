import type { Friend } from '../friends-list';

// TODO(api:user): replace local friends with GET /users/{id}/friends for the authenticated user.
export const friends: Friend[] = [
  {
    id: 'friend-skydogzz',
    name: 'SkyDogzz',
    status: 'online',
    accent: 'yellow',
    note: 'Ready for ranked'
  },
  {
    id: 'friend-add',
    name: 'add',
    status: 'online',
    accent: 'aqua',
    note: 'In lobby'
  },
  {
    id: 'friend-um4ss',
    name: 'um4ss',
    status: 'idle',
    accent: 'lime',
    note: 'Away'
  },
  {
    id: 'friend-vanta',
    name: 'Vanta',
    status: 'offline',
    accent: 'lavender',
    note: 'Last seen today'
  }
];
