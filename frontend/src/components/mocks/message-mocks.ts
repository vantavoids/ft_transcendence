import type { ChatMessageData } from '../chat-message';

// TODO(api:chat,user): load channel history with GET /chat/channels/{channel_id}/messages
// and DM history with GET /chat/dms/{user_id}/messages, then hydrate author profiles.
export const initialMessages: Record<string, ChatMessageData[]> = {
  general: [
    {
      id: '1',
      author: 'um4ss',
      accent: 'lime',
      content: ['Lorem ipsum dolor sit amet, consectetur adipiscing elit.'],
      timestamp: '20:01'
    },
    {
      id: '2',
      author: 'add',
      accent: 'aqua',
      content: ['Lorem ipsum dolor sit amet, consectetur adipiscing elit.'],
      timestamp: '20:03'
    },
    {
      id: '3',
      author: 'SkyDogzz',
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
      author: 'Vanta',
      accent: 'lavender',
      content: ['Lorem ipsum dolor sit amet, consectetur adipiscing elit.'],
      timestamp: '20:11'
    }
  ],
  idk: [
    {
      id: '5',
      author: 'Cartoone',
      accent: 'pink',
      content: ['Canal de test prêt pour les messages locaux.'],
      timestamp: '20:15'
    }
  ],
  ideas_are_tough: [
    {
      id: '6',
      author: 'um4ss',
      accent: 'lime',
      content: ['Brainstorm ici.'],
      timestamp: '20:17'
    }
  ],
  'dm-skydogzz': [
    {
      id: 'dm-skydogzz-1',
      author: 'SkyDogzz',
      accent: 'yellow',
      content: ['On teste la nouvelle view DM ici.'],
      timestamp: '19:42'
    },
    {
      id: 'dm-skydogzz-2',
      author: 'cartoone',
      accent: 'pink',
      content: ['Oui, la colonne de gauche doit juste lister les DMs.'],
      timestamp: '19:44'
    }
  ],
  'dm-add': [
    {
      id: 'dm-add-1',
      author: 'add',
      accent: 'aqua',
      content: ['Je passe après le build.'],
      timestamp: '18:12'
    }
  ],
  'dm-um4ss': [
    {
      id: 'dm-um4ss-1',
      author: 'um4ss',
      accent: 'lime',
      content: ['Ping quand tu peux.'],
      timestamp: '17:58'
    }
  ],
  'dm-vanta': [
    {
      id: 'dm-vanta-1',
      author: 'Vanta',
      accent: 'lavender',
      content: ['Archive de conversation.'],
      timestamp: '15:21'
    }
  ],
  'dm-cartoone': [
    {
      id: 'dm-cartoone-1',
      author: 'cartoone',
      accent: 'pink',
      content: ['Notes personnelles.'],
      timestamp: '12:04'
    }
  ]
};
