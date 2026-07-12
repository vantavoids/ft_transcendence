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
import { ActionModal } from '../action-modal';
import { formatDate } from './guild-overview';
import { getGuildAccentClasses } from './guild-icon';
import { MemberRoleChips, MemberRolesPopover } from './member-roles-popover';
import { useToast } from '../../shared/ui/toast';

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
  onRequestKick: (member: HydratedGuildMember) => void;
  onRequestBan: (member: HydratedGuildMember) => void;
};

function MemberRow({
  guildId,
  member,
  roles,
  caller,
  onChanged,
  onError,
  onRequestKick,
  onRequestBan
}: MemberRowProps) {
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
                onClick={() => onRequestKick(member)}
                disabled={isBusy}
                className={iconButtonClasses}
                aria-label={`Kick ${member.displayName}`}
                title="Kick member"
              >
                <UserX className="h-4 w-4 text-orange" strokeWidth={1.9} />
              </button>
              <button
                type="button"
                onClick={() => onRequestBan(member)}
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
  const [kickTarget, setKickTarget] = useState<HydratedGuildMember | null>(null);
  const [banTarget, setBanTarget] = useState<HydratedGuildMember | null>(null);
  const [banReason, setBanReason] = useState('');
  const [isActionBusy, setIsActionBusy] = useState(false);
  const { pushToast } = useToast();

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

  useEffect(() => {
    if (actionError) {
      pushToast({
        title: 'Members',
        description: actionError,
        tone: 'error'
      });
    }
  }, [actionError, pushToast]);

  useEffect(() => {
    if (error) {
      pushToast({
        title: 'Members',
        description: error,
        tone: 'error'
      });
    }
  }, [error, pushToast]);

  async function confirmKickMember() {
    if (!kickTarget) {
      return;
    }

    setActionError('');

    try {
      setIsActionBusy(true);
      await kickGuildMember(guildId, kickTarget.userId);
      void refresh();
      setKickTarget(null);
    } catch (kickError) {
      setActionError(kickError instanceof Error ? kickError.message : 'Failed to kick member.');
    } finally {
      setIsActionBusy(false);
    }
  }

  async function confirmBanMember() {
    if (!banTarget) {
      return;
    }

    setActionError('');

    try {
      setIsActionBusy(true);
      await banGuildMember(guildId, banTarget.userId, banReason.trim() || undefined);
      void refresh();
      setBanTarget(null);
      setBanReason('');
    } catch (banError) {
      setActionError(banError instanceof Error ? banError.message : 'Failed to ban member.');
    } finally {
      setIsActionBusy(false);
    }
  }

  return (
    <div className="grid gap-3">
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
              onRequestKick={setKickTarget}
              onRequestBan={(target) => {
                setBanTarget(target);
                setBanReason('');
              }}
            />
          ))}
        </ul>
      )}

      {kickTarget ? (
        <ActionModal
          title={`Kick ${kickTarget.displayName}?`}
          description="This will remove the member from the guild."
          confirmLabel="Kick member"
          destructive
          isBusy={isActionBusy}
          onClose={() => setKickTarget(null)}
          onConfirm={confirmKickMember}
        />
      ) : null}

      {banTarget ? (
        <ActionModal
          title={`Ban ${banTarget.displayName}?`}
          description="This will remove the member from the guild and block them from rejoining until unbanned."
          confirmLabel="Ban member"
          destructive
          isBusy={isActionBusy}
          onClose={() => {
            setBanTarget(null);
            setBanReason('');
          }}
          onConfirm={confirmBanMember}
        >
          <label className="grid gap-2">
            <span className="text-sm font-semibold text-white/70">Reason (optional)</span>
            <textarea
              value={banReason}
              onChange={(event) => setBanReason(event.target.value)}
              rows={4}
              maxLength={512}
              placeholder="Optional moderation note"
              className="min-h-[7rem] w-full rounded-md border border-transparent bg-input-bg px-4 py-3 text-base text-white outline-none transition placeholder:text-input-placeholder focus:border-aqua/35"
            />
          </label>
        </ActionModal>
      ) : null}
    </div>
  );
}
