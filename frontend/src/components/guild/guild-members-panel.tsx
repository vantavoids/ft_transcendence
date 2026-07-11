'use client';

import { useMemo, useRef, useState } from 'react';
import Image from 'next/image';
import { Check, Crown, Gavel, Pencil, Shield, UserX, X } from 'lucide-react';
import {
  banGuildMember,
  kickGuildMember,
  updateGuildMemberNickname,
  type GuildRoleDto
} from '../../shared/api/guild';
import { useGuilds } from '../../shared/guilds/guild-store';
import { useGuildMembers, type HydratedGuildMember } from '../../shared/guilds/use-guild-members';
import {
  canManageMemberRoles,
  effectivePermissions,
  memberRank,
  type RoleCaller
} from '../../shared/guilds/role-permissions';
import { formatDate } from './guild-overview';
import { getGuildAccentClasses } from './guild-icon';
import { FormError } from './guild-forms';
import { MemberRoleChips, MemberRolesPopover } from './member-roles-popover';

const iconButtonClasses =
  'flex h-8 w-8 items-center justify-center rounded-md text-[#8b8b8f] transition hover:bg-frame hover:text-white';

function MemberAvatar({ member }: { member: HydratedGuildMember }) {
  if (member.avatarUrl) {
    return (
      <Image
        src={member.avatarUrl}
        alt=""
        width={40}
        height={40}
        unoptimized
        className="h-10 w-10 shrink-0 rounded-full object-cover"
      />
    );
  }

  return (
    <span
      className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-full text-sm font-bold ${getGuildAccentClasses(
        member.userId
      )}`}
    >
      {member.displayName.slice(0, 1).toUpperCase()}
    </span>
  );
}

type MemberRowProps = {
  guildId: string;
  member: HydratedGuildMember;
  roles: GuildRoleDto[];
  caller: RoleCaller | null;
  onChanged: () => void;
  onError: (message: string) => void;
};

function MemberRow({ guildId, member, roles, caller, onChanged, onError }: MemberRowProps) {
  const [isEditingNickname, setIsEditingNickname] = useState(false);
  const [isRolesOpen, setIsRolesOpen] = useState(false);
  const [nicknameDraft, setNicknameDraft] = useState(member.nickname ?? '');
  const [isBusy, setIsBusy] = useState(false);
  const rolesContainerRef = useRef<HTMLDivElement>(null);

  async function run(action: () => Promise<unknown>, fallbackMessage: string) {
    try {
      setIsBusy(true);
      await action();
      onChanged();
    } catch (actionError) {
      onError(actionError instanceof Error ? actionError.message : fallbackMessage);
    } finally {
      setIsBusy(false);
    }
  }

  async function handleSaveNickname() {
    const nickname = nicknameDraft.trim();
    await run(
      () => updateGuildMemberNickname(guildId, member.userId, nickname || null),
      'Failed to update nickname.'
    );
    setIsEditingNickname(false);
  }

  function handleKick() {
    if (!window.confirm(`Kick ${member.displayName} from the guild?`)) {
      return;
    }

    void run(() => kickGuildMember(guildId, member.userId), 'Failed to kick member.');
  }

  function handleBan() {
    const reason = window.prompt(`Ban ${member.displayName}? Optional reason:`);
    if (reason === null) {
      return;
    }

    void run(
      () => banGuildMember(guildId, member.userId, reason.trim() || undefined),
      'Failed to ban member.'
    );
  }

  return (
    <li className="flex items-center gap-3 rounded-md border border-stroke bg-panel px-3 py-2.5">
      <MemberAvatar member={member} />
      <div className="min-w-0 flex-1">
        {isEditingNickname ? (
          <div className="flex items-center gap-2">
            <input
              value={nicknameDraft}
              onChange={(event) => setNicknameDraft(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === 'Enter') {
                  void handleSaveNickname();
                }
                if (event.key === 'Escape') {
                  setIsEditingNickname(false);
                }
              }}
              maxLength={64}
              placeholder="nickname (empty to clear)"
              className="h-8 w-full max-w-[14rem] rounded-md border border-transparent bg-input-bg px-3 text-sm text-white outline-none focus:border-aqua/35"
            />
            <button
              type="button"
              onClick={() => void handleSaveNickname()}
              disabled={isBusy}
              className={iconButtonClasses}
              aria-label="Save nickname"
            >
              <Check className="h-4 w-4 text-lime" strokeWidth={2} />
            </button>
            <button
              type="button"
              onClick={() => setIsEditingNickname(false)}
              className={iconButtonClasses}
              aria-label="Cancel nickname edit"
            >
              <X className="h-4 w-4" strokeWidth={2} />
            </button>
          </div>
        ) : (
          <>
            <p className="flex items-center gap-1.5 truncate text-[0.95rem] font-bold text-white">
              {member.displayName}
              {member.isOwner ? (
                <Crown className="h-3.5 w-3.5 shrink-0 text-yellow" strokeWidth={1.9} />
              ) : null}
            </p>
            <p className="truncate text-xs text-white/35">
              {member.isDeleted
                ? 'deleted user'
                : member.username
                  ? `@${member.username}`
                  : 'Member'}
              {member.nickname ? ` · nickname: ${member.nickname}` : ''}
              {` · joined ${formatDate(member.joinedAt)}`}
            </p>
            <MemberRoleChips roles={member.roles} />
          </>
        )}
      </div>
      {!isEditingNickname ? (
        <div className="flex shrink-0 items-center gap-1">
          {caller ? (
            <div className="relative" ref={rolesContainerRef}>
              <button
                type="button"
                onClick={() => setIsRolesOpen((open) => !open)}
                disabled={isBusy}
                className={iconButtonClasses}
                aria-label={`Manage roles of ${member.displayName}`}
                title="Manage roles"
              >
                <Shield className="h-4 w-4 text-aqua" strokeWidth={1.9} />
              </button>
              {isRolesOpen ? (
                <MemberRolesPopover
                  guildId={guildId}
                  member={member}
                  roles={roles}
                  caller={caller}
                  containerRef={rolesContainerRef}
                  onChanged={onChanged}
                  onClose={() => setIsRolesOpen(false)}
                />
              ) : null}
            </div>
          ) : null}
          <button
            type="button"
            onClick={() => {
              setNicknameDraft(member.nickname ?? '');
              setIsEditingNickname(true);
            }}
            disabled={isBusy}
            className={iconButtonClasses}
            aria-label={`Edit nickname of ${member.displayName}`}
            title="Edit nickname"
          >
            <Pencil className="h-4 w-4" strokeWidth={1.9} />
          </button>
          {!member.isOwner ? (
            <>
              <button
                type="button"
                onClick={handleKick}
                disabled={isBusy}
                className={iconButtonClasses}
                aria-label={`Kick ${member.displayName}`}
                title="Kick member"
              >
                <UserX className="h-4 w-4 text-orange" strokeWidth={1.9} />
              </button>
              <button
                type="button"
                onClick={handleBan}
                disabled={isBusy}
                className={iconButtonClasses}
                aria-label={`Ban ${member.displayName}`}
                title="Ban member"
              >
                <Gavel className="h-4 w-4 text-pink" strokeWidth={1.9} />
              </button>
            </>
          ) : null}
        </div>
      ) : null}
    </li>
  );
}

type GuildMembersPanelProps = {
  guildId: string;
};

export function GuildMembersPanel({ guildId }: GuildMembersPanelProps) {
  const { selectedGuild, currentUserId } = useGuilds();
  const { members, roles, isLoading, error, refresh } = useGuildMembers(
    guildId,
    selectedGuild?.id === guildId ? selectedGuild.owner_id : null
  );
  const [actionError, setActionError] = useState('');

  // Gate the role controls to callers the server would authorize; the caller
  // missing from the loaded members (pagination edge) safely hides them.
  const caller = useMemo<RoleCaller | null>(() => {
    const callerMember = members.find((member) => member.userId === currentUserId);
    if (!callerMember) {
      return null;
    }

    const permissions = effectivePermissions(callerMember.roles, roles, callerMember.isOwner);
    if (!canManageMemberRoles(permissions, callerMember.isOwner)) {
      return null;
    }

    return {
      rank: memberRank(callerMember.roles, callerMember.isOwner),
      permissions,
      isOwner: callerMember.isOwner
    };
  }, [members, roles, currentUserId]);

  return (
    <div className="grid gap-3">
      {actionError ? <FormError message={actionError} /> : null}
      {error ? <FormError message={error} /> : null}
      {isLoading && members.length === 0 ? (
        <div className="h-32 animate-pulse rounded-md bg-panel" />
      ) : (
        <ul className="grid gap-2">
          {members.map((member) => (
            <MemberRow
              key={member.userId}
              guildId={guildId}
              member={member}
              roles={roles}
              caller={caller}
              onChanged={() => {
                setActionError('');
                void refresh();
              }}
              onError={setActionError}
            />
          ))}
        </ul>
      )}
    </div>
  );
}
