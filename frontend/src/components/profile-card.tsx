'use client';

import { useEffect } from 'react';
import {
  Handshake,
  MessageCircle,
  Shield,
  ShieldAlert,
  UserMinus,
  UserPlus,
  UserRound,
  X
} from 'lucide-react';
import { getAccentClasses } from './chat-message';
import { getDmStatusClasses, type DirectMessage } from './dm-list';
import type { GuildMember } from './guild-member-list';
import type { PublicUserProfileDto, RelationshipDto, RelationshipStatus } from '../shared/api/user';

type ProfileCardBaseProps = {
  variant?: 'modal' | 'side';
  onClose: () => void;
};

type GuildMemberProfileCardProps = ProfileCardBaseProps & {
  member: GuildMember;
  user?: never;
  relationship?: never;
  onMessage?: never;
  onAddFriend?: never;
  onCancelRequest?: never;
  onAcceptRequest?: never;
  onRejectRequest?: never;
  onBlock?: never;
  onUnblock?: never;
};

type UserProfileCardProps = ProfileCardBaseProps & {
  user: PublicUserProfileDto;
  relationship: RelationshipDto | null;
  member?: never;
  onMessage?: () => void;
  onAddFriend?: () => void;
  onCancelRequest?: () => void;
  onAcceptRequest?: () => void;
  onRejectRequest?: () => void;
  onBlock?: () => void;
  onUnblock?: () => void;
};

type ProfileCardProps = GuildMemberProfileCardProps | UserProfileCardProps;

function isGuildMemberProfileCard(props: ProfileCardProps): props is GuildMemberProfileCardProps {
  return 'member' in props && props.member !== undefined;
}

function isUserProfileCard(props: ProfileCardProps): props is UserProfileCardProps {
  return 'user' in props && props.user !== undefined;
}

function getStatusDotClasses(status: DirectMessage['status'] | PublicUserProfileDto['status']) {
  switch (status) {
    case 'online':
      return 'bg-lime';
    case 'idle':
      return 'bg-yellow';
    default:
      return 'bg-muted';
  }
}

function getStatusIcon(status: RelationshipStatus) {
  switch (status) {
    case 'accepted':
      return <Handshake className="h-4 w-4" strokeWidth={1.8} />;
    case 'blocked_by_me':
    case 'blocked_by_them':
      return <ShieldAlert className="h-4 w-4" strokeWidth={1.8} />;
    default:
      return <Shield className="h-4 w-4" strokeWidth={1.8} />;
  }
}

function getRelationshipLabel(status: RelationshipStatus | undefined) {
  switch (status) {
    case 'accepted':
      return 'Friends';
    case 'pending_incoming':
      return 'Incoming request';
    case 'pending_outgoing':
      return 'Outgoing request';
    case 'blocked_by_me':
      return 'Blocked by you';
    case 'blocked_by_them':
      return 'Blocked by them';
    default:
      return 'No relationship';
  }
}

function getRelationshipActions(props: UserProfileCardProps) {
  const status = props.relationship?.status ?? 'none';

  switch (status) {
    case 'accepted':
      return {
        primaryLabel: 'Message',
        primaryIcon: MessageCircle,
        primaryAction: props.onMessage,
        secondaryLabel: 'Block',
        secondaryIcon: UserMinus,
        secondaryAction: props.onBlock
      };
    case 'pending_outgoing':
      return {
        primaryLabel: 'Cancel request',
        primaryIcon: UserMinus,
        primaryAction: props.onCancelRequest,
        secondaryLabel: 'Block',
        secondaryIcon: ShieldAlert,
        secondaryAction: props.onBlock
      };
    case 'pending_incoming':
      return {
        primaryLabel: 'Accept',
        primaryIcon: UserPlus,
        primaryAction: props.onAcceptRequest,
        secondaryLabel: 'Decline',
        secondaryIcon: UserMinus,
        secondaryAction: props.onRejectRequest
      };
    case 'blocked_by_me':
      return {
        primaryLabel: 'Unblock',
        primaryIcon: UserPlus,
        primaryAction: props.onUnblock,
        secondaryLabel: 'Message',
        secondaryIcon: MessageCircle,
        secondaryAction: props.onMessage
      };
    case 'blocked_by_them':
      return {
        primaryLabel: 'Blocked',
        primaryIcon: ShieldAlert,
        primaryAction: undefined,
        secondaryLabel: undefined,
        secondaryIcon: undefined,
        secondaryAction: undefined
      };
    default:
      return {
        primaryLabel: 'Add friend',
        primaryIcon: UserPlus,
        primaryAction: props.onAddFriend,
        secondaryLabel: 'Block',
        secondaryIcon: ShieldAlert,
        secondaryAction: props.onBlock
      };
  }
}

function formatLastSeen(lastSeenAt: string | null) {
  if (!lastSeenAt) {
    return 'Last seen unknown';
  }

  const date = new Date(lastSeenAt);
  if (Number.isNaN(date.getTime())) {
    return 'Last seen unknown';
  }

  return `Last seen ${date.toLocaleString('fr-FR', {
    day: '2-digit',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit'
  })}`;
}

function getProfileName(user: PublicUserProfileDto) {
  return user.display_name?.trim() || user.username;
}

function UserProfileContent(props: UserProfileCardProps) {
  const { user, relationship } = props;
  const actions = getRelationshipActions(props);
  const displayName = getProfileName(user);
  const statusLabel = getRelationshipLabel(relationship?.status);
  const PrimaryIcon = actions.primaryIcon;
  const SecondaryIcon = actions.secondaryIcon;

  return (
    <section
      className={`relative w-full overflow-hidden rounded-[1rem] bg-secondary-bg shadow-2xl shadow-black/50 ring-1 ring-white/10 ${
        props.variant === 'modal' ? 'max-w-[23rem]' : 'min-h-0 max-w-none'
      }`}
    >
      <div
        className="h-28 bg-cover bg-center"
        style={
          user.banner_url
            ? {
                backgroundImage: `linear-gradient(180deg, rgba(8, 10, 14, 0.2), rgba(8, 10, 14, 0.8)), url(${user.banner_url})`
              }
            : { backgroundImage: 'linear-gradient(135deg, #1a1a1c 0%, #27333a 46%, #78dce8 100%)' }
        }
      />

      <button
        type="button"
        onClick={props.onClose}
        className="absolute right-4 top-4 flex h-9 w-9 items-center justify-center rounded-md bg-black/35 text-white/70 transition hover:bg-black/55 hover:text-white"
        aria-label="Close profile"
      >
        <X className="h-4 w-4" strokeWidth={2} />
      </button>

      <div className="px-5 pb-5">
        <div className="-mt-10 flex items-end justify-between gap-4">
          <span className="relative shrink-0">
            {user.avatar_url ? (
              <img
                src={user.avatar_url}
                alt={displayName}
                className="h-20 w-20 rounded-full border-4 border-secondary-bg object-cover"
              />
            ) : (
              <span
                className={`flex h-20 w-20 items-center justify-center rounded-full border-4 border-secondary-bg text-3xl font-bold ${getAccentClasses(
                  'aqua'
                )}`}
              >
                {displayName.slice(0, 1).toUpperCase()}
              </span>
            )}
            <span
              className={`absolute bottom-1 right-1 h-4 w-4 rounded-full border-2 border-secondary-bg ${getStatusDotClasses(
                user.status
              )}`}
            />
          </span>
          <span className="font-category mb-2 rounded-full border border-white/10 bg-panel px-3 py-1 text-[0.68rem] uppercase tracking-[0.14em] text-white/45">
            {statusLabel}
          </span>
        </div>

        <div className="mt-4">
          <h2 className="truncate text-[1.6rem] font-bold tracking-[-0.05em] text-white">
            {displayName}
          </h2>
          <p className="mt-1 text-sm text-white/45">@{user.username}</p>
        </div>

        <div className="mt-5 grid gap-3 sm:grid-cols-2">
          <div className="rounded-md border border-white/8 bg-panel px-3 py-3">
            <div className="flex items-center gap-2 text-aqua">
              <Shield className="h-4 w-4" strokeWidth={1.8} />
              <span className="font-category text-[0.68rem] uppercase tracking-[0.14em]">
                Status
              </span>
            </div>
            <p className="mt-2 truncate text-sm font-semibold capitalize text-white">
              {user.status}
            </p>
          </div>
          <div className="rounded-md border border-white/8 bg-panel px-3 py-3">
            <div className="flex items-center gap-2 text-yellow">
              <UserRound className="h-4 w-4" strokeWidth={1.8} />
              <span className="font-category text-[0.68rem] uppercase tracking-[0.14em]">
                Last seen
              </span>
            </div>
            <p className="mt-2 truncate text-sm font-semibold text-white">
              {formatLastSeen(user.last_seen_at)}
            </p>
          </div>
        </div>

        {user.bio ? (
          <div className="mt-4 rounded-md border border-white/8 bg-panel px-3 py-3">
            <p className="font-category text-[0.68rem] uppercase tracking-[0.14em] text-white/45">
              Bio
            </p>
            <p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-white/70">{user.bio}</p>
          </div>
        ) : null}

        <div className="mt-4 rounded-md border border-white/8 bg-panel px-3 py-3">
          <div className="flex items-center gap-2 text-aqua">
            {getStatusIcon(relationship?.status ?? 'none')}
            <span className="font-category text-[0.68rem] uppercase tracking-[0.14em]">
              Relationship
            </span>
          </div>
          <p className="mt-2 text-sm font-semibold text-white">{statusLabel}</p>
          {relationship?.since ? (
            <p className="mt-1 text-xs text-white/35">
              Since {new Date(relationship.since).toLocaleDateString('fr-FR')}
            </p>
          ) : null}
        </div>

        <div className="mt-5 grid gap-2.5">
          {actions.primaryAction ? (
            <button
              type="button"
              onClick={actions.primaryAction}
              className="flex h-11 w-full items-center justify-center gap-2 rounded-md bg-aqua text-sm font-bold text-primary-bg transition hover:bg-white"
            >
              {PrimaryIcon ? <PrimaryIcon className="h-4 w-4" strokeWidth={2} /> : null}
              {actions.primaryLabel}
            </button>
          ) : (
            <button
              type="button"
              disabled
              className="flex h-11 w-full items-center justify-center gap-2 rounded-md bg-frame text-sm font-bold text-white/35"
            >
              {PrimaryIcon ? <PrimaryIcon className="h-4 w-4" strokeWidth={2} /> : null}
              {actions.primaryLabel}
            </button>
          )}

          {actions.secondaryAction && actions.secondaryLabel ? (
            <button
              type="button"
              onClick={actions.secondaryAction}
              className="flex h-11 w-full items-center justify-center gap-2 rounded-md border border-white/10 bg-frame text-sm font-semibold text-white/80 transition hover:border-aqua/40 hover:text-white"
            >
              {SecondaryIcon ? <SecondaryIcon className="h-4 w-4" strokeWidth={2} /> : null}
              {actions.secondaryLabel}
            </button>
          ) : null}
        </div>
      </div>
    </section>
  );
}

function GuildMemberContent({ member, variant = 'modal', onClose }: GuildMemberProfileCardProps) {
  const card = (
    <section
      className={`relative w-full overflow-hidden rounded-[1rem] bg-secondary-bg shadow-2xl shadow-black/50 ring-1 ring-white/10 ${
        variant === 'modal' ? 'max-w-[23rem]' : 'min-h-0 max-w-none'
      }`}
    >
      <div className="h-24 bg-[linear-gradient(135deg,#1a1a1c_0%,#27333a_46%,#78dce8_100%)]" />
      <button
        type="button"
        onClick={onClose}
        className="absolute right-4 top-4 flex h-9 w-9 items-center justify-center rounded-md bg-black/35 text-white/70 transition hover:bg-black/55 hover:text-white"
        aria-label="Close profile"
      >
        <X className="h-4 w-4" strokeWidth={2} />
      </button>

      <div className="px-5 pb-5">
        <div className="-mt-10 flex items-end justify-between gap-4">
          <span className="relative shrink-0">
            <span
              className={`flex h-20 w-20 items-center justify-center rounded-full border-4 border-secondary-bg text-3xl font-bold ${getAccentClasses(
                member.accent
              )}`}
            >
              {member.name.slice(0, 1).toUpperCase()}
            </span>
            <span
              className={`absolute bottom-1 right-1 h-4 w-4 rounded-full border-2 border-secondary-bg ${getDmStatusClasses(
                member.status
              )}`}
            />
          </span>
          <span className="font-category mb-2 rounded-full border border-white/10 bg-panel px-3 py-1 text-[0.68rem] uppercase tracking-[0.14em] text-white/45">
            {member.role}
          </span>
        </div>

        <div className="mt-4">
          <h2 className="truncate text-[1.6rem] font-bold tracking-[-0.05em] text-white">
            {member.name}
          </h2>
          <p className="mt-1 text-sm text-white/45">{member.activity}</p>
        </div>

        <div className="mt-5 grid grid-cols-2 gap-3">
          <div className="rounded-md border border-white/8 bg-panel px-3 py-3">
            <div className="flex items-center gap-2 text-aqua">
              <Shield className="h-4 w-4" strokeWidth={1.8} />
              <span className="font-category text-[0.68rem] uppercase tracking-[0.14em]">
                Status
              </span>
            </div>
            <p className="mt-2 truncate text-sm font-semibold capitalize text-white">
              {member.status}
            </p>
          </div>
          <div className="rounded-md border border-white/8 bg-panel px-3 py-3">
            <div className="flex items-center gap-2 text-yellow">
              <UserRound className="h-4 w-4" strokeWidth={1.8} />
              <span className="font-category text-[0.68rem] uppercase tracking-[0.14em]">
                Guild
              </span>
            </div>
            <p className="mt-2 truncate text-sm font-semibold text-white">server_name</p>
          </div>
        </div>

        <button
          type="button"
          className="mt-5 flex h-11 w-full items-center justify-center gap-2 rounded-md bg-aqua text-sm font-bold text-primary-bg transition hover:bg-white"
        >
          <MessageCircle className="h-4 w-4" strokeWidth={2} />
          Message
        </button>
      </div>
    </section>
  );

  if (variant === 'side') {
    return (
      <aside className="hidden min-h-0 w-[20rem] shrink-0 overflow-hidden xl:flex">{card}</aside>
    );
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/55 px-4 py-6">
      <button
        type="button"
        className="absolute inset-0 cursor-default"
        onClick={onClose}
        aria-label="Close profile"
      />
      {card}
    </div>
  );
}

export function ProfileCard(props: ProfileCardProps) {
  const { onClose } = props;

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

  if (isGuildMemberProfileCard(props)) {
    return <GuildMemberContent member={props.member} variant={props.variant} onClose={onClose} />;
  }

  if (isUserProfileCard(props)) {
    return (
      <UserProfileContent
        user={props.user}
        relationship={props.relationship}
        variant={props.variant}
        onClose={onClose}
        onMessage={props.onMessage}
        onAddFriend={props.onAddFriend}
        onCancelRequest={props.onCancelRequest}
        onAcceptRequest={props.onAcceptRequest}
        onRejectRequest={props.onRejectRequest}
        onBlock={props.onBlock}
        onUnblock={props.onUnblock}
      />
    );
  }

  return null;
}
