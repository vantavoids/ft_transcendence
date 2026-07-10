'use client';

import { useEffect, useMemo, useRef, useState } from 'react';
import Link from 'next/link';
import {
  Archive,
  Bell,
  Check,
  CircleEllipsis,
  Headphones,
  Mic,
  MicOff,
  Search,
  Settings,
  UserPlus,
  UserRound,
  X
} from 'lucide-react';
import { getAccentClasses, type ChatMessageData } from './chat-message';
import { FriendsList } from './friends-list';
import { friends } from './mocks/friend-mocks';

export type DirectMessage = {
  id: string;
  name: string;
  status: 'online' | 'idle' | 'offline';
  accent: ChatMessageData['accent'];
  lastMessage: string;
  lastMessageAt: string;
  lastActivityAt: number;
  unreadCount: number;
  isArchived: boolean;
};

type DmListProps = {
  activeDm: string;
  directMessages: DirectMessage[];
  showArchived: boolean;
  mobilePane: 'channels' | 'messages';
  username: string;
  isMicMuted: boolean;
  isDeafened: boolean;
  onToggleDeafen: () => void;
  onToggleMicMute: () => void;
  onOpenNotifications: () => void;
  onOpenSettings: () => void;
  onSelectDm: (dmId: string) => void;
  onToggleShowArchived: () => void;
  onArchiveDm: (dmId: string) => void;
};

export function getDmName(dmId: string, dms: DirectMessage[]) {
  if (dmId.length === 0)
    return dmId;
  return dms.find((dm) => dm.id === dmId)?.name ?? dmId;
}

export function getDmDetails(dmId: string, dms: DirectMessage[]) {
  return dms.find((dm) => dm.id === dmId) ?? null;
}

export function hasDm(dmId: string, dms: DirectMessage[]) {
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
  showArchived,
  mobilePane,
  username,
  isMicMuted,
  isDeafened,
  onToggleDeafen,
  onToggleMicMute,
  onOpenNotifications,
  onOpenSettings,
  onSelectDm,
  onToggleShowArchived,
  onArchiveDm
}: DmListProps) {
  const [search, setSearch] = useState('');
  const [activeView, setActiveView] = useState<'dms' | 'friends'>('dms');
  const [confirmingArchiveId, setConfirmingArchiveId] = useState<string | null>(null);
  const confirmArchiveTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    return () => {
      if (confirmArchiveTimeoutRef.current) clearTimeout(confirmArchiveTimeoutRef.current);
    };
  }, []);

  function armArchiveConfirm(dmId: string) {
    setConfirmingArchiveId(dmId);
    if (confirmArchiveTimeoutRef.current) clearTimeout(confirmArchiveTimeoutRef.current);
    // first click only arms the action - it auto-cancels after a few seconds
    confirmArchiveTimeoutRef.current = setTimeout(() => setConfirmingArchiveId(null), 3000);
  }

  function cancelArchiveConfirm() {
    setConfirmingArchiveId(null);
    if (confirmArchiveTimeoutRef.current) clearTimeout(confirmArchiveTimeoutRef.current);
  }

  const filteredDms = useMemo(() => {
    const term = search.trim().toLowerCase();

    const sortedDms = [...directMessages].sort(
      (first, second) => second.lastActivityAt - first.lastActivityAt
    );

    if (!term) {
      return sortedDms;
    }

    return sortedDms.filter(
      (dm) => dm.name.toLowerCase().includes(term) || dm.lastMessage.toLowerCase().includes(term)
    );
  }, [directMessages, search]);

  const hasAnyDms = directMessages.length > 0;
  const isFriendsView = activeView === 'friends';

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
            {isFriendsView ? 'Friends' : 'Direct Messages'}
          </h2>
          <div className="flex shrink-0 items-center gap-3 text-[#8c8c90]">
            <UserRound className="h-5 w-5" strokeWidth={1.8} />
            <CircleEllipsis className="h-5 w-5" strokeWidth={1.8} />
          </div>
        </div>

        <div className="mt-5 grid grid-cols-2 gap-2 rounded-md bg-panel p-1">
          <button
            type="button"
            onClick={() => {
              setActiveView('dms');
              setSearch('');
            }}
            className={`flex h-9 items-center justify-center gap-2 rounded text-sm font-bold transition ${
              !isFriendsView ? 'bg-frame text-white' : 'text-white/40 hover:text-white'
            }`}
          >
            <UserRound className="h-4 w-4" strokeWidth={1.8} />
            DMs
          </button>
          <button
            type="button"
            onClick={() => {
              setActiveView('friends');
              setSearch('');
            }}
            className={`flex h-9 items-center justify-center gap-2 rounded text-sm font-bold transition ${
              isFriendsView ? 'bg-frame text-white' : 'text-white/40 hover:text-white'
            }`}
          >
            <UserPlus className="h-4 w-4" strokeWidth={1.8} />
            Friends
          </button>
        </div>

        {isFriendsView ? null : (
          <>
            <label className="mt-4 flex h-11 items-center gap-3 rounded-md bg-panel px-4 text-muted">
              <Search className="h-4 w-4 shrink-0" strokeWidth={1.75} />
              <input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Search DMs"
                className="mono-detail w-full min-w-0 bg-transparent text-xl text-white outline-none placeholder:text-muted"
              />
            </label>
            <button
              type="button"
              onClick={onToggleShowArchived}
              className={`mt-2 flex items-center gap-2 text-xs font-semibold transition ${
                showArchived ? 'text-aqua' : 'text-white/35 hover:text-white'
              }`}
              aria-pressed={showArchived}
            >
              <Archive className="h-3.5 w-3.5" strokeWidth={1.8} />
              {showArchived ? 'Hide archived' : 'Show archived'}
            </button>
          </>
        )}
      </div>

      {isFriendsView ? (
        <FriendsList friends={friends} search={search} onSearchChange={setSearch} />
      ) : (
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
                const isConfirmingArchive = confirmingArchiveId === dm.id;

                return (
                  <div
                    key={dm.id}
                    onMouseLeave={cancelArchiveConfirm}
                    className={`group relative flex h-[4.75rem] w-full items-center rounded-lg transition ${
                      isActive ? 'bg-frame text-white' : 'text-grey-link hover:bg-frame/60'
                    } ${dm.isArchived ? 'opacity-50' : ''}`}
                  >
                    <button
                      type="button"
                      onClick={() => onSelectDm(dm.id)}
                      className="flex min-w-0 flex-1 items-center gap-3 px-3 py-2 text-left"
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
                    {dm.isArchived ? null : isConfirmingArchive ? (
                      <span className="mr-2 flex shrink-0 items-center gap-1">
                        <button
                          type="button"
                          onClick={(event) => {
                            event.stopPropagation();
                            cancelArchiveConfirm();
                          }}
                          className="flex h-8 w-8 items-center justify-center rounded-md text-[#8b8b8f] transition hover:bg-frame hover:text-white"
                          aria-label="Cancel archive"
                        >
                          <X className="h-4 w-4" strokeWidth={1.8} />
                        </button>
                        <button
                          type="button"
                          onClick={(event) => {
                            event.stopPropagation();
                            cancelArchiveConfirm();
                            onArchiveDm(dm.id);
                          }}
                          className="flex h-8 w-8 items-center justify-center rounded-md text-pink transition hover:bg-pink/15"
                          aria-label={`Confirm archiving conversation with ${dm.name}`}
                        >
                          <Check className="h-4 w-4" strokeWidth={2} />
                        </button>
                      </span>
                    ) : (
                      <button
                        type="button"
                        onClick={(event) => {
                          event.stopPropagation();
                          armArchiveConfirm(dm.id);
                        }}
                        className="mr-2 hidden h-8 w-8 shrink-0 items-center justify-center rounded-md text-[#8b8b8f] transition hover:bg-frame hover:text-pink group-hover:flex"
                        aria-label={`Archive conversation with ${dm.name}`}
                      >
                        <Archive className="h-4 w-4" strokeWidth={1.8} />
                      </button>
                    )}
                  </div>
                );
              })}
            </div>
          )}
        </div>
      )}

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
              className={`transition ${
                isMicMuted ? 'text-pink hover:text-[#ff8aa7]' : 'text-[#8b8b8f] hover:text-white'
              }`}
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
              className={`transition ${
                isDeafened ? 'text-pink hover:text-[#ff8aa7]' : 'text-[#8b8b8f] hover:text-white'
              }`}
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
            <button
              type="button"
              onClick={onOpenSettings}
              className="text-[#8b8b8f] transition hover:text-white"
              aria-label="Open settings"
            >
              <Settings className="h-6 w-6" strokeWidth={1.8} />
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
