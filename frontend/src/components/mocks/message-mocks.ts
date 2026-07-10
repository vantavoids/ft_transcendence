import type { ChatMessageData } from '../chat-message';

// TODO(api:chat,user): load channel history with GET /chat/channels/{channel_id}/messages
// and DM history with GET /chat/dms/{user_id}/messages, then hydrate author profiles.
export const initialMessages: Record<string, ChatMessageData[]> = {
  general: [
    {
      id: '1',
      author: 'user-gamma',
      accent: 'lime',
      content: ['Lorem ipsum dolor sit amet, consectetur adipiscing elit.'],
      timestamp: '20:01'
    },
    {
      id: '2',
      author: 'user-beta',
      accent: 'aqua',
      content: ['Lorem ipsum dolor sit amet, consectetur adipiscing elit.'],
      timestamp: '20:03'
    },
    {
      id: '3',
      author: 'user-alpha',
      accent: 'yellow',
      content: [
        'Lorem ipsum dolor sit amet, consectetur adipiscing elit.',
        'Lorem ipsum dolor sit amet, consectetur adipiscing elit.',
        'Lorem ipsum dolor sit amet, consectetur adipiscing elit.'
      ],
      timestamp: '20:08'
    },
    {
      id: '4',
      author: 'user-delta',
      accent: 'lavender',
      content: ['Lorem ipsum dolor sit amet, consectetur adipiscing elit.'],
      timestamp: '20:11'
    }
  ],
  idk: [
    {
      id: '5',
      author: 'user-epsilon',
      accent: 'pink',
      content: ['Canal de test prêt pour les messages locaux.'],
      timestamp: '20:15'
    }
  ],
  ideas_are_tough: [
    {
      id: '6',
      author: 'user-gamma',
      accent: 'lime',
      content: ['Brainstorm ici.'],
      timestamp: '20:17'
    }
  ],
  'dm-user-alpha': [
    {
      id: 'dm-user-alpha-1',
      author: 'user-alpha',
      accent: 'yellow',
      content: ['On teste la nouvelle view DM ici.'],
      timestamp: '19:42'
    },
    {
      id: 'dm-user-alpha-2',
      author: 'user-epsilon',
      accent: 'pink',
      content: ['Oui, la colonne de gauche doit juste lister les DMs.'],
      timestamp: '19:44'
    }
  ],
  'dm-user-beta': [
    {
      id: 'dm-user-beta-1',
      author: 'user-beta',
      accent: 'aqua',
      content: ['Je passe après le build.'],
      timestamp: '18:12'
    }
  ],
  'dm-user-gamma': [
    {
      id: 'dm-user-gamma-1',
      author: 'user-gamma',
      accent: 'lime',
      content: ['Ping quand tu peux.'],
      timestamp: '17:58'
    }
  ],
  'dm-user-delta': [
    {
      id: 'dm-user-delta-1',
      author: 'user-delta',
      accent: 'lavender',
      content: ['Archive de conversation.'],
      timestamp: '15:21'
    }
  ],
  'dm-user-epsilon': [
    {
      id: 'dm-user-epsilon-1',
      author: 'user-epsilon',
      accent: 'pink',
      content: ['Notes personnelles.'],
      timestamp: '12:04'
    }
  ]
};
