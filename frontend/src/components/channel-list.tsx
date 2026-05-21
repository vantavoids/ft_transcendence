'use client';

import { useMemo, useState } from 'react';
import Link from 'next/link';
import {
  CircleEllipsis,
  Bell,
  Hash,
  Headphones,
  MicOff,
  Search,
  Settings,
  UserRound
} from 'lucide-react';

export type TextChannel = {
  id: string;
  name: string;
};

type ChannelCategory = {
  id: string;
  name: string;
  channels: TextChannel[];
};

type ChannelListProps = {
  activeChannel: string;
  mobilePane: 'channels' | 'messages';
  username: string;
  onOpenNotifications: () => void;
  onSelectChannel: (channelId: string) => void;
};

export const channelCategories: ChannelCategory[] = [
  {
    id: 'welcome',
    name: 'Start Here',
    channels: [
      { id: 'general', name: 'general' },
      { id: 'rules', name: 'rules' },
      { id: 'announcements', name: 'announcements' },
      { id: 'introductions', name: 'introductions' }
    ]
  },
  {
    id: 'transcendence',
    name: 'Transcendence',
    channels: [
      { id: 'matchmaking', name: 'matchmaking' },
      { id: 'tournaments', name: 'tournaments' },
      { id: 'ranked-ladder', name: 'ranked-ladder' },
      { id: 'replays', name: 'replays' },
      { id: 'patch-notes', name: 'patch-notes' }
    ]
  },
  {
    id: 'dev',
    name: 'Dev',
    channels: [
      { id: 'frontend', name: 'frontend' },
      { id: 'backend', name: 'backend' },
      { id: 'infra', name: 'infra' },
      { id: 'api-contracts', name: 'api-contracts' },
      { id: 'bugs', name: 'bugs' },
      { id: 'ideas_are_tough', name: 'ideas_are_tough' }
    ]
  },
  {
    id: 'community',
    name: 'Community',
    channels: [
      { id: 'idk', name: 'idk' },
      { id: 'clips', name: 'clips' },
      { id: 'screenshots', name: 'screenshots' },
      { id: 'music', name: 'music' },
      { id: 'off-topic', name: 'off-topic' }
    ]
  },
  {
    id: 'teams',
    name: 'Teams',
    channels: [
      { id: 'team-alpha', name: 'team-alpha' },
      { id: 'team-beta', name: 'team-beta' },
      { id: 'team-gamma', name: 'team-gamma' },
      { id: 'team-delta', name: 'team-delta' }
    ]
  },
  {
    id: 'archive',
    name: 'Archive',
    channels: [
      { id: 'old-general', name: 'old-general' },
      { id: 'season-one', name: 'season-one' },
      { id: 'season-two', name: 'season-two' },
      { id: 'random-notes', name: 'random-notes' }
    ]
  }
];

export function getChannelName(channelId: string) {
  return (
    channelCategories
      .flatMap((category) => category.channels)
      .find((channel) => channel.id === channelId)?.name ?? channelId
  );
}

export function hasChannel(channelId: string) {
  return channelCategories.some((category) =>
    category.channels.some((channel) => channel.id === channelId)
  );
}

export function ChannelList({
  activeChannel,
  mobilePane,
  username,
  onOpenNotifications,
  onSelectChannel
}: ChannelListProps) {
  const [search, setSearch] = useState('');

  const filteredCategories = useMemo(() => {
    const term = search.trim().toLowerCase();

    if (!term) {
      return channelCategories;
    }

    return channelCategories
      .map((category) => ({
        ...category,
        channels: category.channels.filter((channel) => channel.name.toLowerCase().includes(term))
      }))
      .filter((category) => category.channels.length > 0);
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
            server_name
          </h2>
          <div className="flex shrink-0 items-center gap-3 text-[#8c8c90]">
            <UserRound className="h-5 w-5" strokeWidth={1.8} />
            <CircleEllipsis className="h-5 w-5" strokeWidth={1.8} />
          </div>
        </div>

        <div className="mt-5 h-24 overflow-hidden rounded-lg border border-white/10 bg-[linear-gradient(135deg,#232329_0%,#2b3141_38%,#24545b_68%,#78dce8_100%)] shadow-inner shadow-black/40">
          <div className="h-full w-full bg-[linear-gradient(90deg,rgba(16,16,20,0.68),transparent_58%),radial-gradient(circle_at_78%_30%,rgba(255,216,102,0.5),transparent_26%),radial-gradient(circle_at_60%_78%,rgba(255,97,136,0.38),transparent_24%)]" />
        </div>

        <label className="mt-6 flex h-11 items-center gap-3 rounded-md bg-panel px-4 text-muted">
          <Search className="h-4 w-4 shrink-0" strokeWidth={1.75} />
          <input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Search"
            className="mono-detail w-full min-w-0 bg-transparent text-xl text-white outline-none placeholder:text-muted"
          />
        </label>
      </div>

      <div className="min-h-0 flex-1 overflow-y-auto px-3 pb-5 sm:px-5">
        <div className="space-y-6">
          {filteredCategories.map((category) => (
            <section key={category.id}>
              <p className="font-category px-1 text-[0.78rem] uppercase tracking-[0.16em] text-category">
                {category.name}
              </p>
              <div className="mt-2 space-y-1">
                {category.channels.map((channel) => {
                  const isActive = channel.id === activeChannel;

                  return (
                    <button
                      key={channel.id}
                      type="button"
                      onClick={() => onSelectChannel(channel.id)}
                      className={`mono-detail flex h-10 w-full items-center gap-3 rounded-md px-3 text-left text-[1rem] transition ${
                        isActive ? 'bg-frame text-white' : 'text-grey-link hover:bg-frame/60'
                      }`}
                    >
                      <Hash className="h-4 w-4 shrink-0 text-[#8a8a96]" strokeWidth={1.8} />
                      <span
                        className={`min-w-0 truncate ${isActive ? 'font-bold' : 'font-normal'}`}
                      >
                        {channel.name}
                      </span>
                    </button>
                  );
                })}
              </div>
            </section>
          ))}
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
