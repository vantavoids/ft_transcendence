'use client';

import { useEffect } from 'react';
import { Crown, Shield } from 'lucide-react';
import { AvatarWithStatus } from './avatar-with-status';
import type { ChatMessageData } from './chat-message';
import type { DirectMessage } from './dm-list';
import { guildMembers } from './mocks/guild-member-mocks';
import type { GuildRoleDto } from '../shared/api/guild';
import type { UserStatus } from '../shared/api/user';
import { useGuilds } from '../shared/guilds/guild-store';
import { useGuildMembers, type HydratedGuildMember } from '../shared/guilds/use-guild-members';
import { countPermissionBits, rolePermissionBits } from '../shared/guilds/role-permissions';
import { useGroupMembersByRole } from '../shared/hooks/use-group-members-by-role';
import { accentForId } from '../shared/lib/accent';
import { useToast } from '../shared/ui/toast';

export type GuildMember = {
  id: string;
  name: string;
  /** 'Owner', 'Member', or the member's highest guild role name. */
  role: string;
  /** The highest role's color, used to tint the role token. */
  roleColor?: string | null;
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

export function toProfileMember(member: HydratedGuildMember): GuildMember {
  // roles come sorted by display priority, so [0] is the highest role
  const topRole = member.roles[0] ?? null;

  return {
    id: member.userId,
    name: member.displayName,
    role: member.isOwner ? 'Owner' : (topRole?.name ?? 'Member'),
    roleColor: member.isOwner ? null : (topRole?.color ?? null),
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
  onOpenProfile
}: {
  member: HydratedGuildMember;
  onOpenProfile: (member: GuildMember) => void;
}) {
  const topRole = member.isOwner ? 'Owner' : (member.roles[0]?.name ?? null);
  const joined = formatJoinedAt(member.joinedAt);
  const subtitle = [topRole, joined ? `Joined ${joined}` : null].filter(Boolean).join(' · ');

  return (
    <button
      type="button"
      onClick={() => onOpenProfile(toProfileMember(member))}
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

// The guild owner is pinned first in a dedicated "Owner" section no matter
// what roles they hold. Everyone else sections under their top role (most
// permissions first, matching the display priority of the roles themselves);
// role-less members go last under "Members". With grouping off, everyone
// merges into one alphabetical list.
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

    const topRole = member.roles[0] ?? null;
    const key = topRole?.id ?? 'members';
    const group = groupsByRole.get(key) ?? { role: topRole, members: [] };
    group.members.push(member);
    groupsByRole.set(key, group);
  }

  const roleGroups = [...groupsByRole.values()]
    .sort((a, b) => {
      if (!a.role || !b.role) {
        return a.role ? -1 : b.role ? 1 : 0;
      }

      const byPermissionCount =
        countPermissionBits(rolePermissionBits(b.role)) -
        countPermissionBits(rolePermissionBits(a.role));
      if (byPermissionCount !== 0) {
        return byPermissionCount;
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

type GuildMemberListProps = {
  onOpenProfile: (member: GuildMember) => void;
};

export function GuildMemberList({ onOpenProfile }: GuildMemberListProps) {
  const { selectedGuild } = useGuilds();
  const { members, isLoading, error } = useGuildMembers(
    selectedGuild?.id ?? null,
    selectedGuild?.owner_id ?? null
  );
  const groupByRole = useGroupMembersByRole();
  const { pushToast } = useToast();

  useEffect(() => {
    if (error) {
      pushToast({
        title: 'Members',
        description: error,
        tone: 'error'
      });
    }
  }, [error, pushToast]);

  const memberGroups = buildMemberGroups(members, groupByRole);
  const onlineCount = members.filter((member) => member.status !== 'offline').length;

  return (
    <aside className="hidden min-h-0 w-[18rem] shrink-0 flex-col overflow-hidden rounded-[1rem] bg-secondary-bg ring-1 ring-stroke xl:flex">
      <div className="flex h-[4.9rem] shrink-0 items-center justify-between border-b border-stroke px-5">
        <div>
          <h2 className="text-[1.05rem] font-bold tracking-[-0.03em] text-white">Members</h2>
          <p className="font-category mt-1 text-[0.7rem] uppercase tracking-[0.14em] text-white/35">
            {onlineCount} online · {members.length - onlineCount} offline
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
                    <MemberRow key={member.userId} member={member} onOpenProfile={onOpenProfile} />
                  ))}
                </div>
              </section>
            ))}
          </div>
        )}
      </div>
    </aside>
  );
}
