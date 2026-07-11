'use client';

import { Crown, Shield } from 'lucide-react';
import { AvatarWithStatus } from './avatar-with-status';
import type { ChatMessageData } from './chat-message';
import type { DirectMessage } from './dm-list';
import { guildMembers } from './mocks/guild-member-mocks';
import type { UserStatus } from '../shared/api/user';
import { useGuilds } from '../shared/guilds/guild-store';
import { useGuildMembers, type HydratedGuildMember } from '../shared/guilds/use-guild-members';
import { hashToIndex } from '../shared/lib/hash';

export type GuildMember = {
  id: string;
  name: string;
  role: 'Owner' | 'Admin' | 'Member';
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

const ACCENTS: ChatMessageData['accent'][] = ['aqua', 'yellow', 'lime', 'lavender', 'pink'];

function getAccentForId(id: string): ChatMessageData['accent'] {
  return ACCENTS[hashToIndex(id, ACCENTS.length)];
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
  return {
    id: member.userId,
    name: member.displayName,
    role: member.isOwner ? 'Owner' : member.roles.length > 0 ? 'Admin' : 'Member',
    status: toSidebarStatus(member.status),
    accent: getAccentForId(member.userId),
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

  if (member.roles.length > 0) {
    return <Shield className="h-3.5 w-3.5 shrink-0 text-aqua" strokeWidth={1.9} />;
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
        accent={getAccentForId(member.userId)}
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

type GuildMemberListProps = {
  onOpenProfile: (member: GuildMember) => void;
};

export function GuildMemberList({ onOpenProfile }: GuildMemberListProps) {
  const { selectedGuild } = useGuilds();
  const { members, isLoading, error } = useGuildMembers(
    selectedGuild?.id ?? null,
    selectedGuild?.owner_id ?? null
  );

  const staff = members.filter((member) => member.isOwner || member.roles.length > 0);
  const regulars = members.filter((member) => !member.isOwner && member.roles.length === 0);
  const memberGroups = [
    { id: 'staff', title: 'Staff', members: staff },
    { id: 'members', title: 'Members', members: regulars }
  ].filter((group) => group.members.length > 0);

  return (
    <aside className="hidden min-h-0 w-[18rem] shrink-0 flex-col overflow-hidden rounded-[1rem] bg-secondary-bg ring-1 ring-white/5 xl:flex">
      <div className="flex h-[4.9rem] shrink-0 items-center justify-between border-b border-white/8 px-5">
        <div>
          <h2 className="text-[1.05rem] font-bold tracking-[-0.03em] text-white">Members</h2>
          <p className="font-category mt-1 text-[0.7rem] uppercase tracking-[0.14em] text-white/35">
            {members.length} online and offline
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
          <p className="rounded-md border border-pink/25 bg-pink/10 px-3 py-2 text-sm text-pink">
            {error}
          </p>
        ) : !selectedGuild ? (
          <p className="px-1 text-sm text-white/35">Select a guild to see its members.</p>
        ) : (
          <div className="space-y-6">
            {memberGroups.map((group) => (
              <section key={group.id}>
                <p className="font-category px-1 text-[0.72rem] uppercase tracking-[0.16em] text-category">
                  {group.title} - {group.members.length}
                </p>
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
