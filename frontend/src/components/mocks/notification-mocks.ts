import type { NotificationItem } from '../notification-card';

// TODO(api:notification): replace local items with GET /notifications?read=false&limit=50.
export const notifications: NotificationItem[] = [
  {
    id: 'match-win',
    title: 'Alerte de partie gagnee',
    detail: '+24 points ajoutes au ladder.',
    time: '2 min',
    tone: 'yellow'
  },
  {
    id: 'guild-invite',
    title: 'Invitation de guilde',
    detail: 'SkyDogzz t invite dans Neon Arena.',
    time: '18 min',
    tone: 'aqua'
  },
  {
    id: 'friend-accepted',
    title: 'Demande ami acceptee',
    detail: 'add est maintenant dans tes contacts.',
    time: '1 h',
    tone: 'pink'
  }
];
