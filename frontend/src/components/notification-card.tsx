'use client';

import { useEffect, useRef, useState } from 'react';
import {
  AtSign,
  Bell,
  BellOff,
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
import type {
  NotificationDto,
  NotificationPreferenceDto,
  NotificationScopeType
} from '../shared/api/notification';
import { getUsersByIds } from '../shared/api/user';
import {
  getNotificationMuteScopes,
  isScopeMuted,
  type NotificationMuteScope,
  type UseNotificationsResult
} from '../shared/lib/use-notifications';
import { useCloseOnEscape } from '../shared/hooks/use-close-on-escape';
import { useToast } from '../shared/ui/toast';

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
  onOpenNotification: (notification: NotificationDto) => void;
};

// which notifications can deep-link somewhere: a DM opens the sender's
// conversation (the actor is the partner), a mention carries its guild/channel
// in the payload, a friend request opens the requests tab, a guild invite
// opens the join form and a welcome opens the guild (source_id is its id)
function hasNotificationTarget(notification: NotificationDto): boolean {
  switch (notification.type) {
    case 'dm':
      return Boolean(notification.actor_id);
    case 'mention':
    case 'friend_request':
    case 'guild_invite':
      return true;
    case 'guild_welcome':
      return Boolean(notification.source_id);
    default:
      return false;
  }
}

function getOpenActionLabel(notification: NotificationDto): string {
  switch (notification.type) {
    case 'friend_request':
      return 'Voir la demande d ami';
    case 'guild_invite':
      return 'Voir l invitation';
    case 'guild_welcome':
      return 'Ouvrir la guilde';
    default:
      return 'Ouvrir la conversation';
  }
}

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

// actorName is the resolved display name behind actor_id (the message author,
// requester, inviter, or caller); null while unresolved, so every branch keeps
// an actor-less fallback.
export function describeNotification(
  notification: NotificationDto,
  actorName: string | null
): NotificationView {
  switch (notification.type) {
    case 'mention':
      return {
        title: actorName ? `Mention from ${actorName}` : 'New mention',
        detail: notification.payload.preview,
        tone: 'aqua',
        Icon: AtSign
      };
    case 'dm':
      return {
        title: actorName ? `Private message from ${actorName}` : 'Private message',
        detail: notification.payload.preview,
        tone: 'aqua',
        Icon: MessageCircle
      };
    case 'friend_request':
      return {
        title: 'Friend request',
        detail: actorName
          ? `${actorName} wants to add you as a friend.`
          : 'Someone wants to add you as a friend.',
        tone: 'pink',
        Icon: UserPlus
      };
    case 'guild_invite':
      return {
        title: 'Guild invite',
        detail: actorName
          ? `${actorName} invited you to ${notification.payload.guild_name}.`
          : `You have been invited to ${notification.payload.guild_name}.`,
        tone: 'yellow',
        Icon: Mail
      };
    case 'guild_welcome':
      return {
        title: 'Welcome',
        detail: `You are now a member of ${notification.payload.guild_name}!`,
        tone: 'yellow',
        Icon: Sparkles
      };
    case 'incoming_call': {
      const kind = notification.payload.call_type === 'video' ? 'video' : 'audio';
      return {
        title: 'Incoming call',
        detail: actorName ? `Incoming ${kind} call from ${actorName}.` : `Incoming ${kind} call.`,
        tone: 'pink',
        Icon: Phone
      };
    }
  }
}

export function formatRelativeTime(isoDate: string): string {
  const timestamp = Date.parse(isoDate);
  if (!Number.isFinite(timestamp)) {
    return '';
  }

  const elapsedMinutes = Math.floor((Date.now() - timestamp) / 60_000);
  if (elapsedMinutes < 1) {
    return 'just now';
  }
  if (elapsedMinutes < 60) {
    return `${elapsedMinutes}m`;
  }
  const elapsedHours = Math.floor(elapsedMinutes / 60);
  if (elapsedHours < 24) {
    return `${elapsedHours}h`;
  }
  const elapsedDays = Math.floor(elapsedHours / 24);
  if (elapsedDays < 7) {
    return `${elapsedDays}d`;
  }
  return new Date(timestamp).toLocaleDateString('en-US');
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
          : 'border-stroke text-white/40 hover:border-stroke-strong hover:text-white/70'
      }`}
    >
      {label}
    </button>
  );
}

function getScopeLabel(scopeType: NotificationScopeType) {
  return scopeType === 'guild' ? 'the guild' : 'the channel';
}

type NotificationRowProps = {
  notification: NotificationDto;
  actorName: string | null;
  preferences: NotificationPreferenceDto[];
  onMarkRead: (notificationId: string) => void;
  onDismiss: (notificationId: string) => void;
  onMute: (scope: NotificationMuteScope) => void;
  onOpen: (notification: NotificationDto) => void;
};

function NotificationRow({
  notification,
  actorName,
  preferences,
  onMarkRead,
  onDismiss,
  onMute,
  onOpen
}: NotificationRowProps) {
  const { title, detail, tone, Icon } = describeNotification(notification, actorName);
  const isDismissed = Boolean(notification.dismissed_at);
  const isClickable = hasNotificationTarget(notification);
  const muteScopes = getNotificationMuteScopes(notification).filter(
    (scope) => !isScopeMuted(preferences, scope.scopeType, scope.scopeId)
  );

  return (
    <article
      className={`relative rounded-md border px-3 py-3 ${
        notification.read ? 'border-stroke' : 'border-aqua/25'
      } bg-panel ${isDismissed ? 'opacity-55' : ''} ${
        isClickable ? 'transition hover:border-aqua/45' : ''
      }`}
    >
      {isClickable ? (
        // full-row overlay link; the action buttons below sit above it via
        // their positioned container, so they keep their own click targets
        <button
          type="button"
          onClick={() => onOpen(notification)}
          className="absolute inset-0 rounded-md"
          aria-label={getOpenActionLabel(notification)}
          title={getOpenActionLabel(notification)}
        />
      ) : null}
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
                <span className="h-1.5 w-1.5 shrink-0 rounded-full bg-aqua" aria-label="Unread" />
              ) : null}
            </h3>
            <div className="relative flex shrink-0 items-center gap-1.5">
              <span className="mono-detail text-xs text-white/30">
                {formatRelativeTime(notification.created_at)}
              </span>
              {!notification.read ? (
                <button
                  type="button"
                  onClick={() => onMarkRead(notification.id)}
                  className="flex h-6 w-6 items-center justify-center rounded text-white/35 transition hover:bg-frame hover:text-aqua"
                  aria-label="Mark as read"
                  title="Mark as read"
                >
                  <Check className="h-3.5 w-3.5" strokeWidth={2} />
                </button>
              ) : null}
              {muteScopes.map((scope) => (
                <button
                  key={`${scope.scopeType}-${scope.scopeId}`}
                  type="button"
                  onClick={() => onMute(scope)}
                  className="flex h-6 w-6 items-center justify-center rounded text-white/35 transition hover:bg-frame hover:text-yellow"
                  aria-label={`Mute ${getScopeLabel(scope.scopeType)}`}
                  title={`Mute ${getScopeLabel(scope.scopeType)}`}
                >
                  <BellOff className="h-3.5 w-3.5" strokeWidth={2} />
                </button>
              ))}
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

const muteDurations = [
  { value: '1h', label: '1h', hours: 1 },
  { value: '8h', label: '8h', hours: 8 },
  { value: '24h', label: '24h', hours: 24 },
  { value: 'forever', label: 'Forever', hours: null }
] as const;

type MuteDuration = (typeof muteDurations)[number]['value'];

function muteDurationToIso(duration: MuteDuration): string | null {
  const hours = muteDurations.find((option) => option.value === duration)?.hours ?? null;
  return hours === null ? null : new Date(Date.now() + hours * 3_600_000).toISOString();
}

function describeMuteState(preference: NotificationPreferenceDto): string {
  if (!preference.muted) {
    return 'Not muted';
  }
  const expiry = preference.muted_until == null ? NaN : Date.parse(preference.muted_until);
  if (Number.isNaN(expiry)) {
    // no expiry or an unparseable one: never render Invalid Date
    return 'No expiry';
  }
  if (expiry <= Date.now()) {
    return 'Expired';
  }
  return `Until ${new Date(expiry).toLocaleString('en-US', {
    dateStyle: 'short',
    timeStyle: 'short'
  })}`;
}

type MutePanelProps = {
  preferences: NotificationPreferenceDto[];
  onMute: (
    scopeType: NotificationScopeType,
    scopeId: string,
    mutedUntil: string | null
  ) => Promise<void>;
  onUnmute: (scopeType: NotificationScopeType, scopeId: string) => Promise<void>;
};

function MutePanel({ preferences, onMute, onUnmute }: MutePanelProps) {
  const [scopeType, setScopeType] = useState<NotificationScopeType>('guild');
  const [scopeId, setScopeId] = useState('');
  const [duration, setDuration] = useState<MuteDuration>('forever');
  const [formError, setFormError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const { pushToast } = useToast();

  useEffect(() => {
    if (formError) {
      pushToast({
        title: 'Notification mutes',
        description: formError,
        tone: 'error'
      });
    }
  }, [formError, pushToast]);

  async function handleAddMute() {
    const trimmedId = scopeId.trim();
    if (!/^\d+$/.test(trimmedId)) {
      setFormError('Enter the guild or channel snowflake ID.');
      return;
    }

    setFormError('');
    setIsSubmitting(true);
    try {
      await onMute(scopeType, trimmedId, muteDurationToIso(duration));
      setScopeId('');
    } catch {
      setFormError('Could not save the mute.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="space-y-4">
      <div className="rounded-md border border-stroke bg-panel px-3 py-3">
        <p className="font-category text-[0.68rem] uppercase tracking-[0.14em] text-white/35">
          New mute
        </p>
        <div className="mt-2.5 flex gap-2">
          {(['guild', 'channel'] as const).map((option) => (
            <button
              key={option}
              type="button"
              onClick={() => setScopeType(option)}
              aria-pressed={scopeType === option}
              className={`h-8 flex-1 rounded-md border text-xs font-semibold transition ${
                scopeType === option
                  ? 'border-aqua/45 bg-aqua/10 text-aqua'
                  : 'border-stroke text-white/40 hover:text-white/70'
              }`}
              >
              {option === 'guild' ? 'Guild' : 'Channel'}
            </button>
          ))}
        </div>
        <input
          value={scopeId}
          onChange={(event) => setScopeId(event.target.value)}
          placeholder={`ID of the ${scopeType === 'guild' ? 'guild' : 'channel'}`}
          className="mono-detail mt-2 h-9 w-full rounded-md bg-input-bg px-3 text-sm text-white outline-none placeholder:text-input-placeholder focus:ring-1 focus:ring-aqua/35"
        />
        <div className="mt-2 flex gap-2">
          {muteDurations.map((option) => (
            <button
              key={option.value}
              type="button"
              onClick={() => setDuration(option.value)}
              aria-pressed={duration === option.value}
              className={`h-7 flex-1 rounded-md border text-[0.68rem] font-semibold transition ${
                duration === option.value
                  ? 'border-yellow/45 bg-yellow/10 text-yellow'
                  : 'border-stroke text-white/40 hover:text-white/70'
              }`}
            >
              {option.label}
            </button>
          ))}
        </div>
        <button
          type="button"
          onClick={handleAddMute}
          disabled={isSubmitting}
          className="mt-2.5 flex h-9 w-full items-center justify-center gap-2 rounded-md bg-frame text-sm font-bold text-white/70 transition hover:bg-white/10 hover:text-white disabled:cursor-not-allowed disabled:opacity-45"
        >
          <BellOff className="h-4 w-4" strokeWidth={1.9} />
          {isSubmitting ? 'Saving...' : 'Mute'}
        </button>
      </div>

      {preferences.length === 0 ? (
        <p className="py-3 text-center text-sm text-white/40">No mutes configured.</p>
      ) : (
        <div className="space-y-2">
          {preferences.map((preference) => (
            <div
              key={`${preference.scope_type}-${preference.scope_id}`}
              className="flex items-center gap-3 rounded-md border border-stroke bg-panel px-3 py-2.5"
            >
              <span
                className={`flex h-8 w-8 shrink-0 items-center justify-center rounded-md border ${
                  preference.muted
                    ? 'border-yellow/30 bg-yellow/10 text-yellow'
                    : 'border-stroke bg-frame text-white/35'
                }`}
              >
                <BellOff className="h-4 w-4" strokeWidth={1.9} />
              </span>
              <div className="min-w-0 flex-1">
                <p className="text-sm font-bold text-white">
                  {preference.scope_type === 'guild' ? 'Guild' : 'Channel'}
                  <span className="mono-detail ml-2 text-xs font-normal text-white/35">
                    {preference.scope_id}
                  </span>
                </p>
                <p className="font-category mt-0.5 text-[0.65rem] uppercase tracking-[0.12em] text-white/35">
                  {describeMuteState(preference)}
                </p>
              </div>
              <button
                type="button"
                onClick={() => {
                  void onUnmute(preference.scope_type, preference.scope_id).catch(() => {});
                }}
                className="flex h-7 shrink-0 items-center gap-1.5 rounded-md border border-stroke px-2.5 text-xs font-semibold text-white/50 transition hover:border-aqua/40 hover:text-aqua"
              >
                <Bell className="h-3.5 w-3.5" strokeWidth={1.9} />
                Unmute
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

export function NotificationCard({ feed, onClose, onOpenNotification }: NotificationCardProps) {
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
    dismiss,
    preferences,
    mute,
    unmute
  } = feed;
  const [view, setView] = useState<'feed' | 'mutes'>('feed');
  const [muteError, setMuteError] = useState('');
  const [actorNamesById, setActorNamesById] = useState<Map<string, string>>(new Map());
  const { pushToast } = useToast();
  // ids already requested (found or not), so deleted actors aren't refetched
  // on every render; rows fall back to actor-less copy for them
  const requestedActorIdsRef = useRef<Set<string>>(new Set());

  useEffect(() => {
    const missingIds = notifications
      .map((notification) => notification.actor_id)
      .filter((actorId): actorId is string => Boolean(actorId))
      .filter((actorId) => !requestedActorIdsRef.current.has(actorId));

    if (missingIds.length === 0) {
      return;
    }

    for (const actorId of missingIds) {
      requestedActorIdsRef.current.add(actorId);
    }

    // no cancellation on cleanup: the merge below is idempotent, and Strict
    // Mode's mount/unmount/mount cycle would otherwise discard the response
    // (the second effect run skips ids the first one already marked requested)
    getUsersByIds(missingIds)
      .then((users) => {
        if (users.length === 0) {
          return;
        }

        setActorNamesById((previous) => {
          const next = new Map(previous);
          for (const user of users) {
            next.set(user.id, user.display_name || user.username);
          }
          return next;
        });
      })
      .catch(() => {
        // best effort: rows keep their actor-less wording
      });
  }, [notifications]);

  useEffect(() => {
    if (error) {
      pushToast({
        title: 'Notifications',
        description: error,
        tone: 'error'
      });
    }
  }, [error, pushToast]);

  useEffect(() => {
    if (muteError) {
      pushToast({
        title: 'Notification mutes',
        description: muteError,
        tone: 'error'
      });
    }
  }, [muteError, pushToast]);

  async function handleRowMute(scope: NotificationMuteScope) {
    setMuteError('');
    try {
      await mute(scope.scopeType, scope.scopeId, null);
    } catch {
      setMuteError('Could not save the mute.');
    }
  }

  useCloseOnEscape(onClose);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/55 px-4 py-6">
      <button
        type="button"
        className="absolute inset-0 cursor-default"
        onClick={onClose}
        aria-label="Close notifications"
      />
      <section className="relative w-full max-w-[25rem] overflow-hidden rounded-[1rem] bg-secondary-bg shadow-2xl shadow-black/50 ring-1 ring-stroke">
        <div className="flex h-[4.75rem] items-center justify-between border-b border-stroke px-5">
          <div className="flex min-w-0 items-center gap-3">
            <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-md bg-aqua/10 text-aqua">
              <Bell className="h-5 w-5" strokeWidth={1.9} />
            </span>
            <div className="min-w-0">
              <h2 className="truncate text-[1.15rem] font-bold tracking-[-0.03em] text-white">
                Notifications
              </h2>
              <p className="font-category text-[0.7rem] uppercase tracking-[0.14em] text-white/35">
                {unreadCount} unread
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

        <div className="flex items-center gap-2 border-b border-stroke px-5 py-3">
          <FilterChip
            active={filter.unreadOnly}
            label="Unread"
            onClick={() => setFilter({ ...filter, unreadOnly: !filter.unreadOnly })}
          />
          <FilterChip
            active={filter.includeDismissed}
            label="Dismissed"
            onClick={() => setFilter({ ...filter, includeDismissed: !filter.includeDismissed })}
          />
          <button
            type="button"
            onClick={() => setView((current) => (current === 'feed' ? 'mutes' : 'feed'))}
            aria-pressed={view === 'mutes'}
            className={`font-category ml-auto flex h-8 items-center gap-1.5 rounded-full border px-3 text-[0.68rem] uppercase tracking-[0.12em] transition ${
              view === 'mutes'
                ? 'border-yellow/45 bg-yellow/10 text-yellow'
                : 'border-stroke text-white/40 hover:border-stroke-strong hover:text-white/70'
            }`}
          >
            <BellOff className="h-3.5 w-3.5" strokeWidth={1.9} />
            Mutes
          </button>
        </div>

        <div className="max-h-[24rem] overflow-y-auto px-4 py-4">
          {view === 'mutes' ? (
            <MutePanel preferences={preferences} onMute={mute} onUnmute={unmute} />
          ) : isLoading ? (
            <div className="space-y-2" aria-label="Loading notifications">
              {[0, 1, 2].map((index) => (
                <div
                  key={index}
                  className="h-16 animate-pulse rounded-md border border-stroke bg-panel"
                />
              ))}
            </div>
          ) : error ? (
            <div className="flex flex-col items-center gap-3 py-8 text-center">
              <p className="text-sm text-white/45">Notifications unavailable.</p>
              <button
                type="button"
                onClick={refresh}
                className="flex h-9 items-center gap-2 rounded-md border border-stroke bg-frame px-4 text-sm font-semibold text-white/70 transition hover:text-white"
              >
                <RotateCw className="h-4 w-4" strokeWidth={1.9} />
                Retry
              </button>
            </div>
          ) : notifications.length === 0 ? (
            <div className="flex flex-col items-center gap-3 py-8 text-center">
              <span className="flex h-12 w-12 items-center justify-center rounded-full bg-panel text-[#8b8b8f]">
                <Bell className="h-5 w-5" strokeWidth={1.8} />
              </span>
              <p className="text-sm text-white/45">No notifications.</p>
            </div>
          ) : (
            <div className="space-y-2">
              {notifications.map((notification) => (
                <NotificationRow
                  key={notification.id}
                  notification={notification}
                  actorName={
                    notification.actor_id
                      ? (actorNamesById.get(notification.actor_id) ?? null)
                      : null
                  }
                  preferences={preferences}
                  onMarkRead={markRead}
                  onDismiss={dismiss}
                  onMute={(scope) => {
                    void handleRowMute(scope);
                  }}
                  onOpen={onOpenNotification}
                />
              ))}
              {hasMore ? (
                <button
                  type="button"
                  onClick={loadMore}
                  disabled={isLoadingMore}
                  className="flex h-10 w-full items-center justify-center rounded-md border border-stroke text-sm font-semibold text-white/50 transition hover:text-white disabled:cursor-not-allowed disabled:opacity-50"
                >
                  {isLoadingMore ? 'Loading...' : 'Load more'}
                </button>
              ) : null}
            </div>
          )}
        </div>

        <div className="border-t border-stroke px-4 py-4">
          <button
            type="button"
            onClick={markAllRead}
            disabled={unreadCount === 0}
            className="flex h-10 w-full items-center justify-center gap-2 rounded-md bg-frame text-sm font-bold text-white/70 transition hover:bg-white/10 hover:text-white disabled:cursor-not-allowed disabled:opacity-45"
          >
            <CheckCheck className="h-4 w-4" strokeWidth={1.9} />
            Mark all as read
          </button>
        </div>
      </section>
    </div>
  );
}
