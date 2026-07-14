'use client';

import { useEffect, useRef, useState } from 'react';
import type { MouseEvent } from 'react';
import { Crown, Shield } from 'lucide-react';
import { AvatarWithStatus } from './avatar-with-status';
import type { ChatMessageData } from './chat-message';
import type { DirectMessage } from './dm-list';
import { guildMembers } from './mocks/guild-member-mocks';
import { ActionModal } from './action-modal';
import type { GuildRoleDto } from '../shared/api/guild';
import type { UserStatus } from '../shared/api/user';
import { useGuilds } from '../shared/guilds/guild-store';
import { useChannelReaders } from '../shared/guilds/use-channel-readers';
import { useGuildMembers, type HydratedGuildMember } from '../shared/guilds/use-guild-members';
import { useCloseOnEscape } from '../shared/hooks/use-close-on-escape';
import { useGroupMembersByRole } from '../shared/hooks/use-group-members-by-role';
import { accentForId } from '../shared/lib/accent';
import { useToast } from '../shared/ui/toast';

export type GuildMemberRole = {
  id: string;
  name: string;
  color: string | null;
};

export type GuildMember = {
  id: string;
  name: string;
  /** 'Owner', 'Member', or the member's highest guild role name. */
  role: string;
  /** The highest role's color, used to tint the role token. */
  roleColor?: string | null;
  /** All assigned (non-@everyone) roles, for the profile card. Absent for non-guild profiles (DMs). */
  roles?: GuildMemberRole[];
  status: DirectMessage['status'];
  accent: ChatMessageData['accent'];
  activity: string;
  bio?: string | null;
  avatarUrl?: string | null;
  bannerUrl?: string | null;
};

// TODO(api:chat): message authors still come from mocks until Epic 4 wires real chat history.
export function getGuildMemberByName(name: string) {
  return guildMembers.find((member) => member.name.toLowerCase() === name.toLowerCase()) ?? null;
}

function toSidebarStatus(status: UserStatus): DirectMessage['status'] {
  return status === 'dnd' ? 'idle' : status;
}

function formatJoinedAt(joinedAt: string) {
  const date = new Date(joinedAt);
  return Number.isNaN(date.getTime())
    ? ''
    : date.toLocaleDateString('en-US', { day: '2-digit', month: 'short', year: 'numeric' });
}

// The member's "top" role for display (name badge / color): the highest by
// hierarchy position, so assigning a higher role promotes the badge to it.
export function topRoleByPosition(roles: GuildRoleDto[]): GuildRoleDto | null {
  let top: GuildRoleDto | null = null;
  for (const role of roles) {
    if (role.is_default) {
      continue;
    }
    if (!top || role.position > top.position) {
      top = role;
    }
  }
  return top;
}

export function toProfileMember(member: HydratedGuildMember): GuildMember {
  const topRole = topRoleByPosition(member.roles);

  return {
    id: member.userId,
    name: member.displayName,
    role: member.isOwner ? 'Owner' : (topRole?.name ?? 'Member'),
    roleColor: member.isOwner ? null : (topRole?.color ?? null),
    // every assigned role (minus @everyone), so the profile card lists them all
    roles: member.roles
      .filter((role) => !role.is_default)
      .map((role) => ({ id: role.id, name: role.name, color: role.color })),
    status: toSidebarStatus(member.status),
    accent: accentForId(member.userId),
    activity: member.joinedAt ? `Joined ${formatJoinedAt(member.joinedAt)}` : 'Member',
    bio: member.bio,
    avatarUrl: member.avatarUrl,
    bannerUrl: member.bannerUrl
  };
}

function RoleIcon({ member }: { member: HydratedGuildMember }) {
  if (member.isOwner) {
    return <Crown className="h-3.5 w-3.5 shrink-0 text-yellow" strokeWidth={1.9} />;
  }

  const topRoleColor = member.roles[0]?.color;
  if (member.roles.length > 0) {
    return (
      <Shield
        className="h-3.5 w-3.5 shrink-0 text-aqua"
        strokeWidth={1.9}
        style={topRoleColor ? { color: topRoleColor } : undefined}
      />
    );
  }

  return null;
}

function MemberRow({
  member,
  onOpenProfile,
  onContextMenu
}: {
  member: HydratedGuildMember;
  onOpenProfile: (member: GuildMember) => void;
  onContextMenu?: (event: MouseEvent, member: HydratedGuildMember) => void;
}) {
  const topRole = member.isOwner ? 'Owner' : (member.roles[0]?.name ?? null);
  const joined = formatJoinedAt(member.joinedAt);
  const subtitle = [topRole, joined ? `Joined ${joined}` : null].filter(Boolean).join(' · ');

  return (
    <button
      type="button"
      onClick={() => onOpenProfile(toProfileMember(member))}
      onContextMenu={onContextMenu ? (event) => onContextMenu(event, member) : undefined}
      className="flex h-14 w-full items-center gap-3 rounded-md px-2 text-left text-grey-link transition hover:bg-frame/60 hover:text-white"
    >
      <AvatarWithStatus
        size="sm"
        name={member.displayName}
        accent={accentForId(member.userId)}
        status={toSidebarStatus(member.status)}
        avatarUrl={member.avatarUrl}
      />
      <span className="min-w-0 flex-1">
        <span className="flex min-w-0 items-center gap-1.5">
          <span className="block truncate text-[0.95rem] font-bold">{member.displayName}</span>
          <RoleIcon member={member} />
        </span>
        <span className="mt-0.5 block truncate text-xs text-white/35">
          {member.isDeleted ? 'Deleted user' : subtitle || 'Member'}
        </span>
      </span>
    </button>
  );
}

export type MemberGroup = {
  id: string;
  title: string;
  roleColor: string | null;
  members: HydratedGuildMember[];
};

// A member's section is their highest HOISTED role (Discord's "Display role
// members separately"); a role with hoisting off never creates a section, so
// its members fall through to their next hoisted role or to "Members". Highest
// = greatest position in the role hierarchy.
function highestHoistedRole(member: HydratedGuildMember): GuildRoleDto | null {
  let best: GuildRoleDto | null = null;
  for (const role of member.roles) {
    if (!role.is_hoisted || role.is_default) {
      continue;
    }
    if (!best || role.position > best.position) {
      best = role;
    }
  }
  return best;
}

// The guild owner is pinned first in a dedicated "Owner" section no matter what
// roles they hold. Everyone else sections under their highest hoisted role
// (highest position first, matching the hierarchy); members with no hoisted
// role go last under "Members". With grouping off, everyone merges into one
// alphabetical list.
export function buildMemberGroups(
  members: HydratedGuildMember[],
  groupByRole: boolean
): MemberGroup[] {
  if (!groupByRole) {
    const merged = [...members].sort((a, b) => a.displayName.localeCompare(b.displayName));
    return merged.length > 0
      ? [{ id: 'all', title: 'Members', roleColor: null, members: merged }]
      : [];
  }

  const owners = members.filter((member) => member.isOwner);
  const groupsByRole = new Map<
    string,
    { role: GuildRoleDto | null; members: HydratedGuildMember[] }
  >();

  for (const member of members) {
    if (member.isOwner) {
      continue;
    }

    const hoisted = highestHoistedRole(member);
    const key = hoisted?.id ?? 'members';
    const group = groupsByRole.get(key) ?? { role: hoisted, members: [] };
    group.members.push(member);
    groupsByRole.set(key, group);
  }

  const roleGroups = [...groupsByRole.values()]
    .sort((a, b) => {
      if (!a.role || !b.role) {
        return a.role ? -1 : b.role ? 1 : 0;
      }
      if (a.role.position !== b.role.position) {
        return b.role.position - a.role.position;
      }
      return a.role.name.localeCompare(b.role.name);
    })
    .map((group) => ({
      id: group.role?.id ?? 'members',
      title: group.role?.name ?? 'Members',
      roleColor: group.role?.color ?? null,
      members: group.members
    }));

  return owners.length > 0
    ? [{ id: 'owner', title: 'Owner', roleColor: null, members: owners }, ...roleGroups]
    : roleGroups;
}

type MemberMenuState = {
  member: HydratedGuildMember;
  x: number;
  y: number;
};

function MemberContextMenu({
  menu,
  onTransferOwnership,
  onClose
}: {
  menu: MemberMenuState;
  onTransferOwnership: (member: HydratedGuildMember) => void;
  onClose: () => void;
}) {
  const menuRef = useRef<HTMLDivElement>(null);

  useCloseOnEscape(onClose);

  useEffect(() => {
    function handleMouseDown(event: globalThis.MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        onClose();
      }
    }

    document.addEventListener('mousedown', handleMouseDown);
    return () => document.removeEventListener('mousedown', handleMouseDown);
  }, [onClose]);

  return (
    <div
      ref={menuRef}
      className="fixed z-50 w-56 rounded-md border border-stroke bg-panel p-1.5 shadow-lg"
      style={{
        left: Math.min(menu.x, window.innerWidth - 240),
        top: Math.min(menu.y, window.innerHeight - 88)
      }}
    >
      <p className="mono-detail truncate px-2 py-1 text-xs text-white/35">{menu.member.displayName}</p>
      <button
        type="button"
        onClick={() => {
          onTransferOwnership(menu.member);
          onClose();
        }}
        className="flex h-9 w-full items-center gap-2.5 rounded-md px-2 text-left text-sm font-semibold text-white/70 transition hover:bg-frame hover:text-white"
      >
        <Crown className="h-4 w-4 shrink-0 text-yellow" strokeWidth={1.9} />
        Transfer ownership
      </button>
    </div>
  );
}

type GuildMemberListProps = {
  activeChannelId: string | null;
  onOpenProfile: (member: GuildMember) => void;
};

export function GuildMemberList({ activeChannelId, onOpenProfile }: GuildMemberListProps) {
  const { selectedGuild, currentUserId, transferOwnership } = useGuilds();
  const { members, isLoading, error, refresh } = useGuildMembers(
    selectedGuild?.id ?? null,
    selectedGuild?.owner_id ?? null
  );
  const readerIds = useChannelReaders(selectedGuild?.id ?? null, activeChannelId);
  const groupByRole = useGroupMembersByRole();
  const { pushToast } = useToast();
  const [memberMenu, setMemberMenu] = useState<MemberMenuState | null>(null);
  const [transferTarget, setTransferTarget] = useState<HydratedGuildMember | null>(null);
  const [isTransferBusy, setIsTransferBusy] = useState(false);

  const isCurrentUserOwner =
    Boolean(currentUserId) && selectedGuild?.owner_id === currentUserId;

  useEffect(() => {
    if (error) {
      pushToast({
        title: 'Members',
        description: error,
        tone: 'error'
      });
    }
  }, [error, pushToast]);

  // owner-only right-click action; never on the owner's own row.
  const handleMemberContextMenu = (event: MouseEvent, member: HydratedGuildMember) => {
    if (!isCurrentUserOwner || member.isOwner || member.isDeleted) {
      return;
    }
    event.preventDefault();
    setMemberMenu({ member, x: event.clientX, y: event.clientY });
  };

  async function confirmTransferOwnership() {
    if (!transferTarget) {
      return;
    }

    try {
      setIsTransferBusy(true);
      await transferOwnership(selectedGuild!.id, transferTarget.userId);
      void refresh();
      setTransferTarget(null);
    } catch (transferError) {
      pushToast({
        title: 'Transfer ownership',
        description:
          transferError instanceof Error ? transferError.message : 'Failed to transfer ownership.',
        tone: 'error'
      });
    } finally {
      setIsTransferBusy(false);
    }
  }

  // scope to the members who can read the active channel (Discord parity); a
  // null reader set means "unknown" (no channel, loading, or lookup failed), in
  // which case we show everyone rather than blanking the list.
  const visibleMembers = readerIds
    ? members.filter((member) => readerIds.has(member.userId))
    : members;

  const memberGroups = buildMemberGroups(visibleMembers, groupByRole);
  const onlineCount = visibleMembers.filter((member) => member.status !== 'offline').length;

  return (
    <aside className="hidden min-h-0 w-[18rem] shrink-0 flex-col overflow-hidden rounded-[1rem] bg-secondary-bg ring-1 ring-stroke xl:flex">
      <div className="flex h-[4.9rem] shrink-0 items-center justify-between border-b border-stroke px-5">
        <div>
          <h2 className="text-[1.05rem] font-bold tracking-[-0.03em] text-white">Members</h2>
          <p className="font-category mt-1 text-[0.7rem] uppercase tracking-[0.14em] text-white/35">
            {onlineCount} online · {visibleMembers.length - onlineCount} offline
          </p>
        </div>
      </div>

      <div className="min-h-0 flex-1 overflow-y-auto px-4 py-5">
        {isLoading && members.length === 0 ? (
          <div className="space-y-2">
            {[0, 1, 2, 3].map((index) => (
              <div key={index} className="h-14 animate-pulse rounded-md bg-frame/50" />
            ))}
          </div>
        ) : error && members.length === 0 ? (
          <p className="rounded-md border border-stroke bg-frame px-3 py-2 text-sm text-white/45">
            Members unavailable.
          </p>
        ) : !selectedGuild ? (
          <p className="px-1 text-sm text-white/35">Select a guild to see its members.</p>
        ) : (
          <div className="space-y-6">
            {memberGroups.map((group) => (
              <section key={group.id}>
                {groupByRole ? (
                  <p className="font-category flex items-center gap-1.5 px-1 text-[0.72rem] uppercase tracking-[0.16em] text-category">
                    {group.roleColor ? (
                      <span
                        className="h-2 w-2 shrink-0 rounded-full"
                        style={{ backgroundColor: group.roleColor }}
                      />
                    ) : null}
                    {group.title} - {group.members.length}
                  </p>
                ) : null}
                <div className="mt-2 space-y-1">
                  {group.members.map((member) => (
                    <MemberRow
                      key={member.userId}
                      member={member}
                      onOpenProfile={onOpenProfile}
                      onContextMenu={isCurrentUserOwner ? handleMemberContextMenu : undefined}
                    />
                  ))}
                </div>
              </section>
            ))}
          </div>
        )}
      </div>

      {memberMenu ? (
        <MemberContextMenu
          menu={memberMenu}
          onTransferOwnership={setTransferTarget}
          onClose={() => setMemberMenu(null)}
        />
      ) : null}

      {transferTarget ? (
        <ActionModal
          title={`Transfer ownership to ${transferTarget.displayName}?`}
          description="They become the guild owner with full control. You keep your membership but lose owner privileges. This can only be undone if the new owner transfers it back."
          confirmLabel="Transfer ownership"
          destructive
          isBusy={isTransferBusy}
          onClose={() => setTransferTarget(null)}
          onConfirm={confirmTransferOwnership}
        />
      ) : null}
    </aside>
  );
}
