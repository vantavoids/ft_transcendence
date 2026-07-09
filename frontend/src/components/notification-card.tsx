'use client';

import { useEffect } from 'react';
import {
  AtSign,
  Bell,
  Check,
  CheckCheck,
  Mail,
  MessageCircle,
  Phone,
  RotateCw,
  Sparkles,
  UserPlus,
  X,
  type LucideIcon
} from 'lucide-react';
import type { NotificationDto } from '../shared/api/notification';
import type { UseNotificationsResult } from '../shared/lib/use-notifications';

type NotificationTone = 'aqua' | 'yellow' | 'pink';

type NotificationView = {
  title: string;
  detail: string;
  tone: NotificationTone;
  Icon: LucideIcon;
};

type NotificationCardProps = {
  feed: UseNotificationsResult;
  onClose: () => void;
};

function getToneClasses(tone: NotificationTone) {
  switch (tone) {
    case 'yellow':
      return 'border-yellow/30 bg-yellow/10 text-yellow';
    case 'pink':
      return 'border-pink/30 bg-pink/10 text-pink';
    default:
      return 'border-aqua/30 bg-aqua/10 text-aqua';
  }
}

function describeNotification(notification: NotificationDto): NotificationView {
  switch (notification.type) {
    case 'mention':
      return {
        title: 'Nouvelle mention',
        detail: notification.payload.preview,
        tone: 'aqua',
        Icon: AtSign
      };
    case 'dm':
      return {
        title: 'Message prive',
        detail: notification.payload.preview,
        tone: 'aqua',
        Icon: MessageCircle
      };
    case 'friend_request':
      return {
        title: 'Demande d ami',
        detail: 'Quelqu un veut t ajouter en ami.',
        tone: 'pink',
        Icon: UserPlus
      };
    case 'guild_invite':
      return {
        title: 'Invitation de guilde',
        detail: `Tu es invite dans ${notification.payload.guild_name}.`,
        tone: 'yellow',
        Icon: Mail
      };
    case 'guild_welcome':
      return {
        title: 'Bienvenue',
        detail: `Te voila membre de ${notification.payload.guild_name} !`,
        tone: 'yellow',
        Icon: Sparkles
      };
    case 'incoming_call':
      return {
        title: 'Appel entrant',
        detail:
          notification.payload.call_type === 'video'
            ? 'Appel video entrant.'
            : 'Appel audio entrant.',
        tone: 'pink',
        Icon: Phone
      };
  }
}

export function formatRelativeTime(isoDate: string): string {
  const timestamp = Date.parse(isoDate);
  if (!Number.isFinite(timestamp)) {
    return '';
  }

  const elapsedMinutes = Math.floor((Date.now() - timestamp) / 60_000);
  if (elapsedMinutes < 1) {
    return 'a l instant';
  }
  if (elapsedMinutes < 60) {
    return `${elapsedMinutes} min`;
  }
  const elapsedHours = Math.floor(elapsedMinutes / 60);
  if (elapsedHours < 24) {
    return `${elapsedHours} h`;
  }
  const elapsedDays = Math.floor(elapsedHours / 24);
  if (elapsedDays < 7) {
    return `${elapsedDays} j`;
  }
  return new Date(timestamp).toLocaleDateString('fr-FR');
}

type FilterChipProps = {
  active: boolean;
  label: string;
  onClick: () => void;
};

function FilterChip({ active, label, onClick }: FilterChipProps) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-pressed={active}
      className={`font-category h-8 rounded-full border px-3 text-[0.68rem] uppercase tracking-[0.12em] transition ${
        active
          ? 'border-aqua/45 bg-aqua/10 text-aqua'
          : 'border-white/10 text-white/40 hover:border-white/25 hover:text-white/70'
      }`}
    >
      {label}
    </button>
  );
}

type NotificationRowProps = {
  notification: NotificationDto;
  onMarkRead: (notificationId: string) => void;
  onDismiss: (notificationId: string) => void;
};

function NotificationRow({ notification, onMarkRead, onDismiss }: NotificationRowProps) {
  const { title, detail, tone, Icon } = describeNotification(notification);
  const isDismissed = Boolean(notification.dismissed_at);

  return (
    <article
      className={`rounded-md border px-3 py-3 ${
        notification.read ? 'border-white/8' : 'border-aqua/25'
      } bg-panel ${isDismissed ? 'opacity-55' : ''}`}
    >
      <div className="flex gap-3">
        <span
          className={`mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-md border ${getToneClasses(
            tone
          )}`}
        >
          <Icon className="h-4 w-4" strokeWidth={1.9} />
        </span>
        <div className="min-w-0 flex-1">
          <div className="flex min-w-0 items-center justify-between gap-3">
            <h3 className="flex min-w-0 items-center gap-2 text-sm font-bold text-white">
              <span className="truncate">{title}</span>
              {!notification.read ? (
                <span className="h-1.5 w-1.5 shrink-0 rounded-full bg-aqua" aria-label="Non lue" />
              ) : null}
            </h3>
            <div className="flex shrink-0 items-center gap-1.5">
              <span className="mono-detail text-xs text-white/30">
                {formatRelativeTime(notification.created_at)}
              </span>
              {!notification.read ? (
                <button
                  type="button"
                  onClick={() => onMarkRead(notification.id)}
                  className="flex h-6 w-6 items-center justify-center rounded text-white/35 transition hover:bg-frame hover:text-aqua"
                  aria-label="Marquer comme lu"
                  title="Marquer comme lu"
                >
                  <Check className="h-3.5 w-3.5" strokeWidth={2} />
                </button>
              ) : null}
              {!isDismissed ? (
                <button
                  type="button"
                  onClick={() => onDismiss(notification.id)}
                  className="flex h-6 w-6 items-center justify-center rounded text-white/35 transition hover:bg-frame hover:text-pink"
                  aria-label="Ignorer la notification"
                  title="Ignorer"
                >
                  <X className="h-3.5 w-3.5" strokeWidth={2} />
                </button>
              ) : null}
            </div>
          </div>
          <p className="mt-1 break-words text-sm leading-5 text-white/45">{detail}</p>
        </div>
      </div>
    </article>
  );
}

export function NotificationCard({ feed, onClose }: NotificationCardProps) {
  const {
    notifications,
    unreadCount,
    hasMore,
    isLoading,
    isLoadingMore,
    error,
    filter,
    setFilter,
    refresh,
    loadMore,
    markRead,
    markAllRead,
    dismiss
  } = feed;

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
                {unreadCount} non lue{unreadCount === 1 ? '' : 's'}
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

        <div className="flex items-center gap-2 border-b border-white/8 px-5 py-3">
          <FilterChip
            active={filter.unreadOnly}
            label="Non lues"
            onClick={() => setFilter({ ...filter, unreadOnly: !filter.unreadOnly })}
          />
          <FilterChip
            active={filter.includeDismissed}
            label="Ignorees"
            onClick={() => setFilter({ ...filter, includeDismissed: !filter.includeDismissed })}
          />
        </div>

        <div className="max-h-[24rem] overflow-y-auto px-4 py-4">
          {isLoading ? (
            <div className="space-y-2" aria-label="Chargement des notifications">
              {[0, 1, 2].map((index) => (
                <div
                  key={index}
                  className="h-16 animate-pulse rounded-md border border-white/8 bg-panel"
                />
              ))}
            </div>
          ) : error ? (
            <div className="flex flex-col items-center gap-3 py-8 text-center">
              <p className="text-sm text-white/45">{error}</p>
              <button
                type="button"
                onClick={refresh}
                className="flex h-9 items-center gap-2 rounded-md border border-white/10 bg-frame px-4 text-sm font-semibold text-white/70 transition hover:text-white"
              >
                <RotateCw className="h-4 w-4" strokeWidth={1.9} />
                Reessayer
              </button>
            </div>
          ) : notifications.length === 0 ? (
            <div className="flex flex-col items-center gap-3 py-8 text-center">
              <span className="flex h-12 w-12 items-center justify-center rounded-full bg-panel text-[#8b8b8f]">
                <Bell className="h-5 w-5" strokeWidth={1.8} />
              </span>
              <p className="text-sm text-white/45">Aucune notification.</p>
            </div>
          ) : (
            <div className="space-y-2">
              {notifications.map((notification) => (
                <NotificationRow
                  key={notification.id}
                  notification={notification}
                  onMarkRead={markRead}
                  onDismiss={dismiss}
                />
              ))}
              {hasMore ? (
                <button
                  type="button"
                  onClick={loadMore}
                  disabled={isLoadingMore}
                  className="flex h-10 w-full items-center justify-center rounded-md border border-white/10 text-sm font-semibold text-white/50 transition hover:text-white disabled:cursor-not-allowed disabled:opacity-50"
                >
                  {isLoadingMore ? 'Chargement...' : 'Charger plus'}
                </button>
              ) : null}
            </div>
          )}
        </div>

        <div className="border-t border-white/8 px-4 py-4">
          <button
            type="button"
            onClick={markAllRead}
            disabled={unreadCount === 0}
            className="flex h-10 w-full items-center justify-center gap-2 rounded-md bg-frame text-sm font-bold text-white/70 transition hover:bg-white/10 hover:text-white disabled:cursor-not-allowed disabled:opacity-45"
          >
            <CheckCheck className="h-4 w-4" strokeWidth={1.9} />
            Tout marquer comme lu
          </button>
        </div>
      </section>
    </div>
  );
}
