'use client';

import { useEffect } from 'react';
import { Bell, CheckCheck, Trophy, UserPlus, X } from 'lucide-react';

type NotificationItem = {
  id: string;
  title: string;
  detail: string;
  time: string;
  tone: 'aqua' | 'yellow' | 'pink';
};

const notifications: NotificationItem[] = [
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

type NotificationCardProps = {
  onClose: () => void;
};

function getToneClasses(tone: NotificationItem['tone']) {
  switch (tone) {
    case 'yellow':
      return 'border-yellow/30 bg-yellow/10 text-yellow';
    case 'pink':
      return 'border-pink/30 bg-pink/10 text-pink';
    default:
      return 'border-aqua/30 bg-aqua/10 text-aqua';
  }
}

function NotificationIcon({ tone }: { tone: NotificationItem['tone'] }) {
  const className = 'h-4 w-4';

  if (tone === 'yellow') {
    return <Trophy className={className} strokeWidth={1.9} />;
  }

  if (tone === 'pink') {
    return <UserPlus className={className} strokeWidth={1.9} />;
  }

  return <Bell className={className} strokeWidth={1.9} />;
}

export function NotificationCard({ onClose }: NotificationCardProps) {
  useEffect(() => {
    function handleEscape(event: KeyboardEvent) {
      if (event.key !== 'Escape' && event.key !== 'Esc' && event.code !== 'Escape') {
        return;
      }

      onClose();
    }

    window.addEventListener('keydown', handleEscape);
    return () => window.removeEventListener('keydown', handleEscape);
  }, [onClose]);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/55 px-4 py-6">
      <button
        type="button"
        className="absolute inset-0 cursor-default"
        onClick={onClose}
        aria-label="Close notifications"
      />
      <section className="relative w-full max-w-[25rem] overflow-hidden rounded-[1rem] bg-secondary-bg shadow-2xl shadow-black/50 ring-1 ring-white/10">
        <div className="flex h-[4.75rem] items-center justify-between border-b border-white/8 px-5">
          <div className="flex min-w-0 items-center gap-3">
            <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-md bg-aqua/10 text-aqua">
              <Bell className="h-5 w-5" strokeWidth={1.9} />
            </span>
            <div className="min-w-0">
              <h2 className="truncate text-[1.15rem] font-bold tracking-[-0.03em] text-white">
                Notifications
              </h2>
              <p className="font-category text-[0.7rem] uppercase tracking-[0.14em] text-white/35">
                {notifications.length} non lues
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="flex h-9 w-9 items-center justify-center rounded-md text-[#8b8b8f] transition hover:bg-frame hover:text-white"
            aria-label="Close notifications"
          >
            <X className="h-4 w-4" strokeWidth={2} />
          </button>
        </div>

        <div className="max-h-[26rem] overflow-y-auto px-4 py-4">
          <div className="space-y-2">
            {notifications.map((notification) => (
              <article
                key={notification.id}
                className="rounded-md border border-white/8 bg-panel px-3 py-3"
              >
                <div className="flex gap-3">
                  <span
                    className={`mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-md border ${getToneClasses(
                      notification.tone
                    )}`}
                  >
                    <NotificationIcon tone={notification.tone} />
                  </span>
                  <div className="min-w-0 flex-1">
                    <div className="flex min-w-0 items-center justify-between gap-3">
                      <h3 className="truncate text-sm font-bold text-white">
                        {notification.title}
                      </h3>
                      <span className="mono-detail shrink-0 text-xs text-white/30">
                        {notification.time}
                      </span>
                    </div>
                    <p className="mt-1 text-sm leading-5 text-white/45">{notification.detail}</p>
                  </div>
                </div>
              </article>
            ))}
          </div>
        </div>

        <div className="border-t border-white/8 px-4 py-4">
          <button
            type="button"
            className="flex h-10 w-full items-center justify-center gap-2 rounded-md bg-frame text-sm font-bold text-white/70 transition hover:bg-white/10 hover:text-white"
          >
            <CheckCheck className="h-4 w-4" strokeWidth={1.9} />
            Marquer comme lu
          </button>
        </div>
      </section>
    </div>
  );
}
