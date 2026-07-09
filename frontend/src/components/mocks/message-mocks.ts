import type { ChatMessageData } from '../chat-message';

// TODO(api:chat,user): load DM history with GET /chat/dms/{user_id}/messages,
// then hydrate author profiles (epic 4 step 4). Channel history is already
// wired to the real API (epic 4 step 3) - no channel-keyed entries remain here.
export const initialMessages: Record<string, ChatMessageData[]> = {
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
