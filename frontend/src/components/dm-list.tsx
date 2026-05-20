'use client';

import { useMemo, useState } from 'react';
import Link from 'next/link';
import { CircleEllipsis, Headphones, MicOff, Search, Settings, UserRound } from 'lucide-react';
import { getAccentClasses, type ChatMessageData } from './chat-message';

export type DirectMessage = {
  id: string;
  name: string;
  status: 'online' | 'idle' | 'offline';
  accent: ChatMessageData['accent'];
  preview: string;
};

type DmListProps = {
  activeDm: string;
  mobilePane: 'channels' | 'messages';
  username: string;
  onSelectDm: (dmId: string) => void;
};

export const directMessages: DirectMessage[] = [
  {
    id: 'dm-skydogzz',
    name: 'SkyDogzz',
    status: 'online',
    accent: 'yellow',
    preview: 'On teste la view DM.'
  },
  {
    id: 'dm-add',
    name: 'add',
    status: 'online',
    accent: 'aqua',
    preview: 'Je passe après le build.'
  },
  {
    id: 'dm-um4ss',
    name: 'um4ss',
    status: 'idle',
    accent: 'lime',
    preview: 'Ping quand tu peux.'
  },
  {
    id: 'dm-vanta',
    name: 'Vanta',
    status: 'offline',
    accent: 'lavender',
    preview: 'Archive de conversation.'
  },
  {
    id: 'dm-cartoone',
    name: 'Cartoone',
    status: 'online',
    accent: 'pink',
    preview: 'Notes personnelles.'
  }
];

export function getDmName(dmId: string) {
  return directMessages.find((dm) => dm.id === dmId)?.name ?? dmId;
}

export function getDmDetails(dmId: string) {
  return directMessages.find((dm) => dm.id === dmId) ?? null;
}

export function hasDm(dmId: string) {
  return directMessages.some((dm) => dm.id === dmId);
}

export function getDmStatusClasses(status: DirectMessage['status']) {
  switch (status) {
    case 'online':
      return 'bg-lime';
    case 'idle':
      return 'bg-yellow';
    default:
      return 'bg-muted';
  }
}

export function DmList({ activeDm, mobilePane, username, onSelectDm }: DmListProps) {
  const [search, setSearch] = useState('');

  const filteredDms = useMemo(() => {
    const term = search.trim().toLowerCase();

    if (!term) {
      return directMessages;
    }

    return directMessages.filter((dm) => dm.name.toLowerCase().includes(term));
  }, [search]);

  return (
    <div
      className={`${
        mobilePane === 'channels' ? 'flex' : 'hidden'
      } min-h-0 flex-1 flex-col overflow-hidden rounded-[1rem] bg-secondary-bg ring-1 ring-white/5 md:flex md:max-w-[25rem]`}
    >
      <div className="shrink-0 px-4 pb-4 pt-4 sm:px-6">
        <div className="flex items-center justify-between gap-4">
          <Link
            href="/"
            className="mono-detail text-[2rem] font-bold tracking-[-0.06em] text-white md:hidden"
          >
            Logo<span className="text-aqua">_</span>
          </Link>
          <h2 className="min-w-0 truncate font-display text-[2rem] font-medium tracking-[-0.05em] text-aqua sm:text-[2.2rem]">
            Direct Messages
          </h2>
          <div className="flex shrink-0 items-center gap-3 text-[#8c8c90]">
            <UserRound className="h-5 w-5" strokeWidth={1.8} />
            <CircleEllipsis className="h-5 w-5" strokeWidth={1.8} />
          </div>
        </div>

        <label className="mt-6 flex h-11 items-center gap-3 rounded-md bg-panel px-4 text-muted">
          <Search className="h-4 w-4 shrink-0" strokeWidth={1.75} />
          <input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Search DMs"
            className="mono-detail w-full min-w-0 bg-transparent text-xl text-white outline-none placeholder:text-muted"
          />
        </label>
      </div>

      <div className="min-h-0 flex-1 overflow-y-auto px-3 pb-5 sm:px-5">
        <div className="space-y-1">
          {filteredDms.map((dm) => {
            const isActive = dm.id === activeDm;

            return (
              <button
                key={dm.id}
                type="button"
                onClick={() => onSelectDm(dm.id)}
                className={`flex h-16 w-full items-center gap-3 rounded-lg px-3 text-left transition ${
                  isActive ? 'bg-frame text-white' : 'text-grey-link hover:bg-frame/60'
                }`}
              >
                <span className="relative shrink-0">
                  <span
                    className={`flex h-11 w-11 items-center justify-center rounded-full text-sm font-bold ${getAccentClasses(
                      dm.accent
                    )}`}
                  >
                    {dm.name.slice(0, 1).toUpperCase()}
                  </span>
                  <span
                    className={`absolute -bottom-0.5 -right-0.5 h-3.5 w-3.5 rounded-full border-2 border-secondary-bg ${getDmStatusClasses(
                      dm.status
                    )}`}
                  />
                </span>
                <span className="min-w-0 flex-1">
                  <span className="block truncate text-[1rem] font-bold">{dm.name}</span>
                  <span className="block truncate text-sm text-white/35">{dm.preview}</span>
                </span>
              </button>
            );
          })}
        </div>
      </div>

      <div className="shrink-0 border-t border-white/8 px-4 py-4">
        <div className="flex items-center justify-between gap-3">
          <div className="flex min-w-0 items-center gap-3">
            <div className="h-12 w-12 shrink-0 overflow-hidden rounded-md bg-[linear-gradient(135deg,#6e7f9d,#d9e2f0)]" />
            <span className="mono-detail min-w-0 truncate text-[2rem] font-medium tracking-[-0.06em] text-white">
              {username}
            </span>
          </div>
          <div className="flex shrink-0 items-center gap-3 text-pink">
            <MicOff className="h-6 w-6" strokeWidth={1.8} />
            <Headphones className="h-6 w-6 text-[#8b8b8f]" strokeWidth={1.8} />
            <Link href="/profile" className="text-[#8b8b8f] transition hover:text-white">
              <Settings className="h-6 w-6" strokeWidth={1.8} />
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
