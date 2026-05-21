'use client';

import { Crown, Shield, UserRound } from 'lucide-react';
import { getAccentClasses, type ChatMessageData } from './chat-message';
import { getDmStatusClasses, type DirectMessage } from './dm-list';

export type GuildMember = {
  id: string;
  name: string;
  role: 'Owner' | 'Admin' | 'Member';
  status: DirectMessage['status'];
  accent: ChatMessageData['accent'];
  activity: string;
};

export const guildMembers: GuildMember[] = [
  {
    id: 'skydogzz',
    name: 'SkyDogzz',
    role: 'Owner',
    status: 'online',
    accent: 'yellow',
    activity: 'In #general'
  },
  {
    id: 'um4ss',
    name: 'um4ss',
    role: 'Admin',
    status: 'idle',
    accent: 'lime',
    activity: 'Watching ladder'
  },
  {
    id: 'add',
    name: 'add',
    role: 'Member',
    status: 'online',
    accent: 'aqua',
    activity: 'Queue ready'
  },
  {
    id: 'cartoone',
    name: 'Cartoone',
    role: 'Member',
    status: 'online',
    accent: 'pink',
    activity: 'Writing'
  },
  {
    id: 'vanta',
    name: 'Vanta',
    role: 'Member',
    status: 'offline',
    accent: 'lavender',
    activity: 'Offline'
  }
];

export function getGuildMemberByName(name: string) {
  return guildMembers.find((member) => member.name.toLowerCase() === name.toLowerCase()) ?? null;
}

const memberGroups = [
  {
    id: 'staff',
    title: 'Staff',
    members: guildMembers.filter((member) => member.role !== 'Member')
  },
  {
    id: 'members',
    title: 'Members',
    members: guildMembers.filter((member) => member.role === 'Member')
  }
];

function RoleIcon({ role }: { role: GuildMember['role'] }) {
  if (role === 'Owner') {
    return <Crown className="h-3.5 w-3.5 text-yellow" strokeWidth={1.9} />;
  }

  if (role === 'Admin') {
    return <Shield className="h-3.5 w-3.5 text-aqua" strokeWidth={1.9} />;
  }

  return null;
}

type GuildMemberListProps = {
  onToggleVisibility: () => void;
  onOpenProfile: (member: GuildMember) => void;
};

export function GuildMemberList({ onToggleVisibility, onOpenProfile }: GuildMemberListProps) {
  return (
    <aside className="hidden min-h-0 w-[18rem] shrink-0 flex-col overflow-hidden rounded-[1rem] bg-secondary-bg ring-1 ring-white/5 xl:flex">
      <div className="flex h-[4.9rem] shrink-0 items-center justify-between border-b border-white/8 px-5">
        <div>
          <h2 className="text-[1.05rem] font-bold tracking-[-0.03em] text-white">Members</h2>
          <p className="font-category mt-1 text-[0.7rem] uppercase tracking-[0.14em] text-white/35">
            {guildMembers.length} online and offline
          </p>
        </div>
        <button
          type="button"
          onClick={onToggleVisibility}
          className="text-aqua transition hover:text-white"
          aria-label="Hide member list"
          aria-pressed
        >
          <UserRound className="h-5 w-5" strokeWidth={1.8} />
        </button>
      </div>

      <div className="min-h-0 flex-1 overflow-y-auto px-4 py-5">
        <div className="space-y-6">
          {memberGroups.map((group) => (
            <section key={group.id}>
              <p className="font-category px-1 text-[0.72rem] uppercase tracking-[0.16em] text-category">
                {group.title} - {group.members.length}
              </p>
              <div className="mt-2 space-y-1">
                {group.members.map((member) => (
                  <button
                    key={member.id}
                    type="button"
                    onClick={() => onOpenProfile(member)}
                    className="flex h-14 w-full items-center gap-3 rounded-md px-2 text-left text-grey-link transition hover:bg-frame/60 hover:text-white"
                  >
                    <span className="relative shrink-0">
                      <span
                        className={`flex h-10 w-10 items-center justify-center rounded-full text-sm font-bold ${getAccentClasses(
                          member.accent
                        )}`}
                      >
                        {member.name.slice(0, 1).toUpperCase()}
                      </span>
                      <span
                        className={`absolute -bottom-0.5 -right-0.5 h-3.5 w-3.5 rounded-full border-2 border-secondary-bg ${getDmStatusClasses(
                          member.status
                        )}`}
                      />
                    </span>
                    <span className="min-w-0 flex-1">
                      <span className="flex min-w-0 items-center gap-1.5">
                        <span className="block truncate text-[0.95rem] font-bold">
                          {member.name}
                        </span>
                        <RoleIcon role={member.role} />
                      </span>
                      <span className="mt-0.5 block truncate text-xs text-white/35">
                        {member.activity}
                      </span>
                    </span>
                  </button>
                ))}
              </div>
            </section>
          ))}
        </div>
      </div>
    </aside>
  );
}
