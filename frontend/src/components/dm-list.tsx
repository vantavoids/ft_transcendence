'use client';

import { useMemo, useState } from 'react';
import Link from 'next/link';
import {
  Bell,
  CircleEllipsis,
  Headphones,
  Mic,
  MicOff,
  Search,
  Settings,
  UserRound
} from 'lucide-react';
import { getAccentClasses, type ChatMessageData } from './chat-message';

export type DirectMessage = {
  id: string;
  name: string;
  status: 'online' | 'idle' | 'offline';
  accent: ChatMessageData['accent'];
  lastMessage: string;
  lastMessageAt: string;
  lastActivityMinutes: number;
  unreadCount: number;
};

type DmListProps = {
  activeDm: string;
  directMessages: DirectMessage[];
  mobilePane: 'channels' | 'messages';
  username: string;
  isMicMuted: boolean;
  isDeafened: boolean;
  onToggleDeafen: () => void;
  onToggleMicMute: () => void;
  onOpenNotifications: () => void;
  onSelectDm: (dmId: string) => void;
};

export const directMessages: DirectMessage[] = [
  {
    id: 'dm-skydogzz',
    name: 'SkyDogzz',
    status: 'online',
    accent: 'yellow',
    lastMessage: 'On teste la view DM.',
    lastMessageAt: '19:44',
    lastActivityMinutes: 19 * 60 + 44,
    unreadCount: 2
  },
  {
    id: 'dm-add',
    name: 'add',
    status: 'online',
    accent: 'aqua',
    lastMessage: 'Je passe après le build.',
    lastMessageAt: '18:12',
    lastActivityMinutes: 18 * 60 + 12,
    unreadCount: 0
  },
  {
    id: 'dm-um4ss',
    name: 'um4ss',
    status: 'idle',
    accent: 'lime',
    lastMessage: 'Ping quand tu peux.',
    lastMessageAt: '17:58',
    lastActivityMinutes: 17 * 60 + 58,
    unreadCount: 1
  },
  {
    id: 'dm-vanta',
    name: 'Vanta',
    status: 'offline',
    accent: 'lavender',
    lastMessage: 'Archive de conversation.',
    lastMessageAt: '15:21',
    lastActivityMinutes: 15 * 60 + 21,
    unreadCount: 0
  },
  {
    id: 'dm-cartoone',
    name: 'Cartoone',
    status: 'online',
    accent: 'pink',
    lastMessage: 'Notes personnelles.',
    lastMessageAt: '12:04',
    lastActivityMinutes: 12 * 60 + 4,
    unreadCount: 0
  }
];

export function getDmName(dmId: string, dms = directMessages) {
  return dms.find((dm) => dm.id === dmId)?.name ?? dmId;
}

export function getDmDetails(dmId: string, dms = directMessages) {
  return dms.find((dm) => dm.id === dmId) ?? null;
}

export function hasDm(dmId: string, dms = directMessages) {
  return dms.some((dm) => dm.id === dmId);
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

export function DmList({
  activeDm,
  directMessages,
  mobilePane,
  username,
  isMicMuted,
  isDeafened,
  onToggleDeafen,
  onToggleMicMute,
  onOpenNotifications,
  onSelectDm
}: DmListProps) {
  const [search, setSearch] = useState('');

  const filteredDms = useMemo(() => {
    const term = search.trim().toLowerCase();

    const sortedDms = [...directMessages].sort(
      (first, second) => second.lastActivityMinutes - first.lastActivityMinutes
    );

    if (!term) {
      return sortedDms;
    }

    return sortedDms.filter(
      (dm) => dm.name.toLowerCase().includes(term) || dm.lastMessage.toLowerCase().includes(term)
    );
  }, [directMessages, search]);

  const hasAnyDms = directMessages.length > 0;

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
        {filteredDms.length === 0 ? (
          <div className="flex h-full min-h-[16rem] flex-col items-center justify-center px-5 text-center">
            <div className="flex h-14 w-14 items-center justify-center rounded-full bg-panel text-[#8b8b8f]">
              <UserRound className="h-6 w-6" strokeWidth={1.8} />
            </div>
            <p className="mt-4 text-[1rem] font-bold text-white">
              {hasAnyDms ? 'No DMs found' : 'No direct messages'}
            </p>
            <p className="mt-1 max-w-[16rem] text-sm leading-5 text-white/35">
              {hasAnyDms
                ? 'Try another name or message preview.'
                : 'Your conversations will appear here.'}
            </p>
          </div>
        ) : (
          <div className="space-y-1">
            {filteredDms.map((dm) => {
              const isActive = dm.id === activeDm;

              return (
                <button
                  key={dm.id}
                  type="button"
                  onClick={() => onSelectDm(dm.id)}
                  className={`flex h-[4.75rem] w-full items-center gap-3 rounded-lg px-3 text-left transition ${
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
                    <span className="flex min-w-0 items-center justify-between gap-3">
                      <span className="block truncate text-[1rem] font-bold">{dm.name}</span>
                      <span className="mono-detail shrink-0 text-xs text-white/30">
                        {dm.lastMessageAt}
                      </span>
                    </span>
                    <span className="mt-0.5 flex min-w-0 items-center justify-between gap-3">
                      <span
                        className={`block truncate text-sm ${
                          dm.unreadCount > 0 ? 'font-semibold text-white/70' : 'text-white/35'
                        }`}
                      >
                        {dm.lastMessage}
                      </span>
                      {dm.unreadCount > 0 ? (
                        <span className="mono-detail flex h-5 min-w-5 shrink-0 items-center justify-center rounded-full bg-pink px-1.5 text-[0.68rem] font-bold text-primary-bg">
                          {dm.unreadCount}
                        </span>
                      ) : null}
                    </span>
                  </span>
                </button>
              );
            })}
          </div>
        )}
      </div>

      <div className="shrink-0 border-t border-white/8 px-4 py-4">
        <div className="flex items-center justify-between gap-3">
          <div className="flex min-w-0 items-center gap-3">
            <div className="h-12 w-12 shrink-0 overflow-hidden rounded-md bg-[linear-gradient(135deg,#6e7f9d,#d9e2f0)]" />
            <span className="mono-detail min-w-0 truncate text-[2rem] font-medium tracking-[-0.06em] text-white">
              {username}
            </span>
          </div>
          <div className="flex shrink-0 items-center gap-3">
            <button
              type="button"
              onClick={onToggleMicMute}
              className={`transition hover:text-white ${isMicMuted ? 'text-pink' : 'text-[#8b8b8f]'}`}
              aria-label={isMicMuted ? 'Unmute microphone' : 'Mute microphone'}
              aria-pressed={isMicMuted}
            >
              {isMicMuted ? (
                <MicOff className="h-6 w-6" strokeWidth={1.8} />
              ) : (
                <Mic className="h-6 w-6" strokeWidth={1.8} />
              )}
            </button>
            <button
              type="button"
              onClick={onToggleDeafen}
              className={`transition hover:text-white ${isDeafened ? 'text-pink' : 'text-[#8b8b8f]'}`}
              aria-label={isDeafened ? 'Undeafen audio' : 'Deafen audio'}
              aria-pressed={isDeafened}
            >
              <Headphones className="h-6 w-6" strokeWidth={1.8} />
            </button>
            <button
              type="button"
              onClick={onOpenNotifications}
              className="relative text-[#8b8b8f] transition hover:text-white"
              aria-label="Show notifications"
            >
              <Bell className="h-6 w-6" strokeWidth={1.8} />
              <span className="absolute -right-0.5 -top-0.5 h-2.5 w-2.5 rounded-full bg-pink" />
            </button>
            <Link href="/profile" className="text-[#8b8b8f] transition hover:text-white">
              <Settings className="h-6 w-6" strokeWidth={1.8} />
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
