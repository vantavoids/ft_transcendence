'use client';

import { useState } from 'react';
import { Search, UserPlus } from 'lucide-react';
import { getAccentClasses, type ChatMessageData } from './chat-message';
import { getDmStatusClasses, type DirectMessage } from './dm-list';

export type Friend = {
  id: string;
  name: string;
  status: DirectMessage['status'];
  accent: ChatMessageData['accent'];
  note: string;
};

type FriendsListProps = {
  friends: Friend[];
  search: string;
  onSearchChange: (value: string) => void;
};

export const friends: Friend[] = [
  {
    id: 'friend-skydogzz',
    name: 'SkyDogzz',
    status: 'online',
    accent: 'yellow',
    note: 'Ready for ranked'
  },
  {
    id: 'friend-add',
    name: 'add',
    status: 'online',
    accent: 'aqua',
    note: 'In lobby'
  },
  {
    id: 'friend-um4ss',
    name: 'um4ss',
    status: 'idle',
    accent: 'lime',
    note: 'Away'
  },
  {
    id: 'friend-vanta',
    name: 'Vanta',
    status: 'offline',
    accent: 'lavender',
    note: 'Last seen today'
  }
];

export function FriendsList({ friends, search, onSearchChange }: FriendsListProps) {
  const [friendName, setFriendName] = useState('');
  const term = search.trim().toLowerCase();
  const filteredFriends = term
    ? friends.filter(
        (friend) =>
          friend.name.toLowerCase().includes(term) || friend.note.toLowerCase().includes(term)
      )
    : friends;

  return (
    <div className="flex min-h-0 flex-1 flex-col">
      <label className="mt-4 flex h-11 items-center gap-3 rounded-md bg-panel px-4 text-muted">
        <Search className="h-4 w-4 shrink-0" strokeWidth={1.75} />
        <input
          value={search}
          onChange={(event) => onSearchChange(event.target.value)}
          placeholder="Search friends"
          className="mono-detail w-full min-w-0 bg-transparent text-xl text-white outline-none placeholder:text-muted"
        />
      </label>

      <div className="min-h-0 flex-1 overflow-y-auto px-3 pb-5 sm:px-5">
        {filteredFriends.length === 0 ? (
          <div className="flex h-full min-h-[16rem] flex-col items-center justify-center px-5 text-center">
            <div className="flex h-14 w-14 items-center justify-center rounded-full bg-panel text-[#8b8b8f]">
              <UserPlus className="h-6 w-6" strokeWidth={1.8} />
            </div>
            <p className="mt-4 text-[1rem] font-bold text-white">No friends found</p>
            <p className="mt-1 max-w-[16rem] text-sm leading-5 text-white/35">
              Try another name or activity.
            </p>
          </div>
        ) : (
          <div className="space-y-1">
            {filteredFriends.map((friend) => (
              <button
                key={friend.id}
                type="button"
                className="flex h-[4.25rem] w-full items-center gap-3 rounded-lg px-3 text-left text-grey-link transition hover:bg-frame/60 hover:text-white"
              >
                <span className="relative shrink-0">
                  <span
                    className={`flex h-11 w-11 items-center justify-center rounded-full text-sm font-bold ${getAccentClasses(
                      friend.accent
                    )}`}
                  >
                    {friend.name.slice(0, 1).toUpperCase()}
                  </span>
                  <span
                    className={`absolute -bottom-0.5 -right-0.5 h-3.5 w-3.5 rounded-full border-2 border-secondary-bg ${getDmStatusClasses(
                      friend.status
                    )}`}
                  />
                </span>
                <span className="min-w-0 flex-1">
                  <span className="block truncate text-[1rem] font-bold">{friend.name}</span>
                  <span className="mt-0.5 block truncate text-sm text-white/35">
                    {friend.note}
                  </span>
                </span>
              </button>
            ))}
          </div>
        )}
      </div>

      <div className="shrink-0 border-t border-white/8 px-4 py-4">
        <div className="flex h-11 items-center gap-2 rounded-md bg-panel px-3 text-muted">
          <input
            value={friendName}
            onChange={(event) => setFriendName(event.target.value)}
            placeholder="Username"
            className="min-w-0 flex-1 bg-transparent text-sm text-white outline-none placeholder:text-muted"
          />
          <button
            type="button"
            disabled={!friendName.trim()}
            className="flex h-8 w-8 shrink-0 items-center justify-center rounded-md bg-aqua text-primary-bg transition hover:bg-white disabled:cursor-not-allowed disabled:bg-frame disabled:text-white/25"
            aria-label="Add friend"
          >
            <UserPlus className="h-4 w-4" strokeWidth={2} />
          </button>
        </div>
      </div>
    </div>
  );
}
