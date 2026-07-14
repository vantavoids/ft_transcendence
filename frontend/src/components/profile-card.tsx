'use client';

import { useEffect, useRef, useState } from 'react';
import { Ban, Check, Menu, MessageCircle, Shield, UserMinus, UserPlus, X } from 'lucide-react';
import { AvatarWithStatus } from './avatar-with-status';
import { topRoleByPosition, type GuildMember, type GuildMemberRole } from './guild-member-list';
import { RoleToggleList } from './guild/member-roles-popover';
import type { GuildRoleDto } from '../shared/api/guild';
import type { RoleCaller } from '../shared/guilds/role-permissions';
import type { HydratedGuildMember } from '../shared/guilds/use-guild-members';
import { useCloseOnEscape } from '../shared/hooks/use-close-on-escape';

// When present, the caller may assign/unassign roles on this member from the
// card. Only supplied for guild members whose viewer holds MANAGE_ROLES.
export type ProfileRoleManagement = {
  guildId: string;
  member: HydratedGuildMember;
  roles: GuildRoleDto[];
  caller: RoleCaller;
  onChanged: () => void;
};

type ProfileCardProps = {
  member: GuildMember;
  variant?: 'modal' | 'side';
  currentUserId?: string | null;
  isBlocked?: boolean;
  isBlockedByThem?: boolean;
  roleManagement?: ProfileRoleManagement;
  onClose: () => void;
  onAddFriend?: (member: GuildMember) => void | Promise<void>;
  onBlock?: (member: GuildMember) => void | Promise<void>;
  onUnblock?: (member: GuildMember) => void | Promise<void>;
  onSendMessage?: (member: GuildMember) => void;
  onUnfriend?: (member: GuildMember) => void;
};

export function ProfileCard({
  member,
  variant = 'modal',
  currentUserId = null,
  isBlocked = false,
  isBlockedByThem = false,
  roleManagement,
  onClose,
  onAddFriend,
  onBlock,
  onUnblock,
  onSendMessage,
  onUnfriend
}: ProfileCardProps) {
  useCloseOnEscape(onClose);
  const [isEditingRoles, setIsEditingRoles] = useState(false);
  const [isActionsOpen, setIsActionsOpen] = useState(false);
  const actionsRef = useRef<HTMLDivElement>(null);
  const isOwnProfile = currentUserId === member.id;

  useEffect(() => {
    if (!isActionsOpen) {
      return;
    }

    function handlePointerDown(event: Event) {
      if (actionsRef.current && !actionsRef.current.contains(event.target as Node)) {
        setIsActionsOpen(false);
      }
    }

    window.addEventListener('mousedown', handlePointerDown);
    window.addEventListener('touchstart', handlePointerDown);

    return () => {
      window.removeEventListener('mousedown', handlePointerDown);
      window.removeEventListener('touchstart', handlePointerDown);
    };
  }, [isActionsOpen]);

  // when the caller can manage roles, derive from the live guild member so the
  // chips AND the top-role badge stay in sync as roles are toggled; otherwise
  // fall back to the snapshot captured when the card opened.
  const liveMember = roleManagement?.member ?? null;

  const displayedRoles: GuildMemberRole[] = liveMember
    ? liveMember.roles
        .filter((role) => !role.is_default)
        .map((role) => ({ id: role.id, name: role.name, color: role.color }))
    : (member.roles ?? []);

  // badge tracks the live highest role (by hierarchy) when managing, so putting a
  // higher role on the member promotes the tag immediately; snapshot otherwise.
  const topRole = liveMember ? topRoleByPosition(liveMember.roles) : null;
  const badgeLabel = liveMember
    ? liveMember.isOwner
      ? 'Owner'
      : (topRole?.name ?? 'Member')
    : member.role;
  const badgeColor = liveMember ? (liveMember.isOwner ? null : (topRole?.color ?? null)) : member.roleColor;

  const card = (
    <section
      className={`relative w-full overflow-hidden rounded-[1rem] bg-secondary-bg shadow-2xl shadow-black/50 ring-1 ring-stroke ${
        variant === 'modal' ? 'max-w-[23rem]' : 'min-h-0 max-w-none'
      }`}
    >
      <div
        className="h-24 bg-[linear-gradient(135deg,#1a1a1c_0%,#27333a_46%,#78dce8_100%)]"
        style={
          member.bannerUrl
            ? {
                backgroundImage: `linear-gradient(135deg, rgba(26,26,28,0.72), rgba(39,51,58,0.48)), url(${member.bannerUrl})`,
                backgroundSize: 'cover',
                backgroundPosition: 'center'
              }
            : undefined
        }
      />
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
          <AvatarWithStatus
            size="lg"
            name={member.name}
            accent={member.accent}
            status={member.status}
            avatarUrl={member.avatarUrl ?? undefined}
          />
          <div className="mb-2 flex max-w-[11rem] items-center gap-2" ref={actionsRef}>
            <span
              className="font-category truncate rounded-full border border-stroke bg-panel px-3 py-1 text-[0.68rem] uppercase tracking-[0.14em] text-white/45"
              style={
                badgeColor
                  ? {
                      color: badgeColor,
                      borderColor: `${badgeColor}59`,
                      backgroundColor: `${badgeColor}1a`
                    }
                  : undefined
              }
              title={badgeLabel}
            >
              {badgeLabel}
            </span>
            {!isOwnProfile && !isBlockedByThem && (onAddFriend || onBlock || onUnblock) ? (
              <div className="relative shrink-0">
                <button
                  type="button"
                  onClick={() => setIsActionsOpen((open) => !open)}
                  className="flex h-9 w-9 items-center justify-center rounded-full border border-stroke bg-panel text-white/55 transition hover:bg-frame hover:text-white"
                  aria-label="Profile actions"
                  aria-expanded={isActionsOpen}
                  aria-haspopup="menu"
                >
                  <Menu className="h-4 w-4" strokeWidth={2} />
                </button>
                {isActionsOpen ? (
                  <div className="absolute right-0 top-full z-10 mt-2 min-w-[12rem] overflow-hidden rounded-md border border-stroke bg-secondary-bg shadow-2xl shadow-black/45">
                    {isBlocked ? null : onAddFriend ? (
                      <button
                        type="button"
                        onClick={() => {
                          setIsActionsOpen(false);
                          void onAddFriend(member);
                        }}
                        className="flex h-10 w-full items-center gap-2 px-3 text-left text-sm font-semibold text-white/75 transition hover:bg-frame hover:text-white"
                        role="menuitem"
                        >
                          <UserPlus className="h-3.5 w-3.5 text-aqua" strokeWidth={2} />
                          Add friend
                        </button>
                    ) : null}
                    {isBlocked && onUnblock ? (
                      <button
                        type="button"
                        onClick={() => {
                          setIsActionsOpen(false);
                          void onUnblock(member);
                        }}
                        className="flex h-10 w-full items-center gap-2 px-3 text-left text-sm font-semibold text-lime transition hover:bg-lime/10"
                        role="menuitem"
                      >
                        <Check className="h-3.5 w-3.5 text-lime" strokeWidth={2} />
                        Unblock
                      </button>
                    ) : null}
                    {!isBlocked && onBlock ? (
                      <button
                        type="button"
                        onClick={() => {
                          setIsActionsOpen(false);
                          void onBlock(member);
                        }}
                        className="flex h-10 w-full items-center gap-2 border-t border-stroke px-3 text-left text-sm font-semibold text-pink transition hover:bg-pink/10"
                        role="menuitem"
                        >
                          <Ban className="h-3.5 w-3.5 text-pink" strokeWidth={2} />
                          Block
                        </button>
                    ) : null}
                  </div>
                ) : null}
              </div>
            ) : null}
          </div>
        </div>

        <div className="mt-4">
          <h2 className="truncate text-[1.6rem] font-bold tracking-[-0.05em] text-white">
            {member.name}
          </h2>
          <p className="mt-1 text-sm text-white/45">{member.activity}</p>
        </div>

        <div className="mt-4 rounded-md border border-stroke bg-panel px-3 py-3">
          <p className="font-category text-[0.68rem] uppercase tracking-[0.14em] text-white/35">
            Bio
          </p>
          <p className="mt-2 text-sm leading-6 text-white/70">
            {member.bio ? member.bio : 'No bio set.'}
          </p>
        </div>

        {displayedRoles.length > 0 || roleManagement ? (
          <div className="mt-4 rounded-md border border-stroke bg-panel px-3 py-3">
            <div className="flex items-center justify-between gap-2">
              <p className="font-category text-[0.68rem] uppercase tracking-[0.14em] text-white/35">
                Roles
              </p>
              {roleManagement ? (
                <button
                  type="button"
                  onClick={() => setIsEditingRoles((open) => !open)}
                  className="flex h-7 items-center gap-1.5 rounded-md px-2 text-xs font-semibold text-white/50 transition hover:bg-frame hover:text-white"
                  aria-label="Manage roles"
                >
                  <Shield className="h-3.5 w-3.5 text-aqua" strokeWidth={1.9} />
                  {isEditingRoles ? 'Done' : 'Manage'}
                </button>
              ) : null}
            </div>

            {displayedRoles.length > 0 ? (
              <div className="mt-2 flex flex-wrap gap-1.5">
                {displayedRoles.map((role) => (
                  <span
                    key={role.id}
                    className="inline-flex items-center gap-1.5 rounded-full border border-stroke bg-frame px-2 py-0.5 text-[0.72rem] font-semibold text-white/70"
                  >
                    <span
                      className="h-2 w-2 shrink-0 rounded-full"
                      style={{ backgroundColor: role.color || '#8b8b8f' }}
                    />
                    {role.name}
                  </span>
                ))}
              </div>
            ) : (
              <p className="mt-2 text-sm text-white/35">No roles assigned.</p>
            )}

            {roleManagement && isEditingRoles ? (
              <div className="mt-3 border-t border-stroke pt-3">
                <RoleToggleList
                  guildId={roleManagement.guildId}
                  member={roleManagement.member}
                  roles={roleManagement.roles}
                  caller={roleManagement.caller}
                  onChanged={roleManagement.onChanged}
                />
              </div>
            ) : null}
          </div>
        ) : null}

        {!isOwnProfile && !isBlocked && !isBlockedByThem && onSendMessage ? (
          <button
            type="button"
            onClick={() => onSendMessage(member)}
            className="mt-5 flex h-11 w-full items-center justify-center gap-2 rounded-md bg-aqua text-sm font-bold text-primary-bg transition hover:bg-white"
          >
            <MessageCircle className="h-4 w-4" strokeWidth={2} />
            Send message
          </button>
        ) : null}

        {!isOwnProfile && onUnfriend ? (
          <button
            type="button"
            onClick={() => onUnfriend(member)}
            className="mt-3 flex h-11 w-full items-center justify-center gap-2 rounded-md border border-pink/40 bg-pink/10 text-sm font-bold text-pink transition hover:bg-pink/20"
          >
            <UserMinus className="h-4 w-4" strokeWidth={2} />
            Unfriend
          </button>
        ) : null}
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
