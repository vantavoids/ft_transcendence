import type { DirectMessage } from '../dm-list';

// TODO(api:chat,user): replace local DMs with GET /chat/dms and hydrate partner profiles via GET /users/{id}.
export const directMessages: DirectMessage[] = [
  {
    id: 'dm-user-alpha',
    name: 'User Alpha',
    status: 'online',
    accent: 'yellow',
    lastMessage: 'On teste la view DM.',
    lastMessageAt: '19:44',
    lastActivityMinutes: 19 * 60 + 44,
    unreadCount: 2
  },
  {
    id: 'dm-user-beta',
    name: 'User Beta',
    status: 'online',
    accent: 'aqua',
    lastMessage: 'Je passe après le build.',
    lastMessageAt: '18:12',
    lastActivityMinutes: 18 * 60 + 12,
    unreadCount: 0
  },
  {
    id: 'dm-user-gamma',
    name: 'User Gamma',
    status: 'idle',
    accent: 'lime',
    lastMessage: 'Ping quand tu peux.',
    lastMessageAt: '17:58',
    lastActivityMinutes: 17 * 60 + 58,
    unreadCount: 1
  },
  {
    id: 'dm-user-delta',
    name: 'User Delta',
    status: 'offline',
    accent: 'lavender',
    lastMessage: 'Archive de conversation.',
    lastMessageAt: '15:21',
    lastActivityMinutes: 15 * 60 + 21,
    unreadCount: 0
  },
  {
    id: 'dm-user-epsilon',
    name: 'User Epsilon',
    status: 'online',
    accent: 'pink',
    lastMessage: 'Notes personnelles.',
    lastMessageAt: '12:04',
    lastActivityMinutes: 12 * 60 + 4,
    unreadCount: 0
  }
];
