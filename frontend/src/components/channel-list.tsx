'use client';

import { useEffect, useMemo, useRef, useState, type RefObject } from 'react';
import Link from 'next/link';
import { CircleEllipsis, Hash, Lock } from 'lucide-react';
import { ConversationListFooter } from './conversation-list-footer';
import { SearchInput } from './search-input';
import { useGuilds } from '../shared/guilds/guild-store';
import { useCloseOnEscape } from '../shared/hooks/use-close-on-escape';
import type { CurrentUserProfile } from '../shared/mappers/user';
import { FortyTwoIcon } from './icons/brand-icons';

export type TextChannel = {
  id: string;
  name: string;
};

export type ChannelCategory = {
  id: string;
  name: string;
  channels: TextChannel[];
};

type ChannelListProps = {
  activeChannel: string;
  categories: ChannelCategory[];
  unreadCounts: Record<string, number>;
  mobilePane: 'channels' | 'messages';
  currentUser: CurrentUserProfile | null;
  isMicMuted: boolean;
  isDeafened: boolean;
  unreadNotifications: number;
  /** Enables the right-click channel menu; keep off for callers the server would 403. */
  canManageChannels?: boolean;
  /** Attached to the bell button so the notification popup can anchor above it. */
  bellRef?: RefObject<HTMLButtonElement | null>;
  onToggleDeafen: () => void;
  onToggleMicMute: () => void;
  onOpenNotifications: () => void;
  onOpenSettings: () => void;
  onOpenGuildSettings: () => void;
  onSelectChannel: (channelId: string) => void;
  onOpenChannelPermissions?: (channel: TextChannel) => void;
};

type ChannelMenuState = {
  channel: TextChannel;
  x: number;
  y: number;
};

function ChannelContextMenu({
  menu,
  onOpenPermissions,
  onClose
}: {
  menu: ChannelMenuState;
  onOpenPermissions: (channel: TextChannel) => void;
  onClose: () => void;
}) {
  const menuRef = useRef<HTMLDivElement>(null);

  useCloseOnEscape(onClose);

  useEffect(() => {
    function handleMouseDown(event: MouseEvent) {
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
      <p className="mono-detail truncate px-2 py-1 text-xs text-white/35"># {menu.channel.name}</p>
      <button
        type="button"
        onClick={() => {
          onOpenPermissions(menu.channel);
          onClose();
        }}
        className="flex h-9 w-full items-center gap-2.5 rounded-md px-2 text-left text-sm font-semibold text-white/70 transition hover:bg-frame hover:text-white"
      >
        <Lock className="h-4 w-4 shrink-0 text-aqua" strokeWidth={1.9} />
        Channel permissions
      </button>
    </div>
  );
}

export function getChannelName(channelId: string, channels: TextChannel[]) {
  if (channelId.length === 0)
    return channelId;
  return channels.find((channel) => channel.id === channelId)?.name ?? channelId;
}

export function hasChannel(channelId: string, channels: TextChannel[]) {
  return channels.some((channel) => channel.id === channelId);
}

export function ChannelList({
  activeChannel,
  categories,
  unreadCounts,
  mobilePane,
  currentUser,
  isMicMuted,
  isDeafened,
  unreadNotifications,
  canManageChannels = false,
  bellRef,
  onToggleDeafen,
  onToggleMicMute,
  onOpenNotifications,
  onOpenSettings,
  onOpenGuildSettings,
  onSelectChannel,
  onOpenChannelPermissions
}: ChannelListProps) {
  const { selectedGuild } = useGuilds();
  const [search, setSearch] = useState('');
  const [channelMenu, setChannelMenu] = useState<ChannelMenuState | null>(null);

  const filteredCategories = useMemo(() => {
    const term = search.trim().toLowerCase();

    if (!term) {
      return categories;
    }

    return categories
      .map((category) => ({
        ...category,
        channels: category.channels.filter((channel) => channel.name.toLowerCase().includes(term))
      }))
      .filter((category) => category.channels.length > 0);
  }, [search, categories]);

  return (
    <div
      className={`${
        mobilePane === 'channels' ? 'flex' : 'hidden'
      } min-h-0 flex-1 flex-col overflow-hidden rounded-[1rem] bg-secondary-bg ring-1 ring-stroke md:flex md:max-w-[25rem]`}
    >
      <div className="shrink-0 px-4 pb-4 pt-4 sm:px-6">
        <div className="flex items-center justify-between gap-4">
          <Link href="/" aria-label="Home" className="text-white md:hidden">
            <FortyTwoIcon className="h-8 w-8" />
          </Link>
          <h2 className="min-w-0 truncate font-display text-[2rem] font-medium tracking-[-0.05em] text-aqua sm:text-[2.2rem]">
            {selectedGuild?.name ?? 'Guild name'}
          </h2>
          <div className="flex shrink-0 items-center gap-3 text-[#8c8c90]">
            <button
              type="button"
              onClick={onOpenGuildSettings}
              className="transition hover:text-white"
              aria-label="Open guild settings"
            >
              <CircleEllipsis className="h-5 w-5" strokeWidth={1.8} />
            </button>
          </div>
        </div>

        <div className="mt-5 h-24 overflow-hidden rounded-lg border border-stroke bg-[linear-gradient(135deg,#232329_0%,#2b3141_38%,#24545b_68%,#78dce8_100%)] shadow-inner shadow-black/40">
          <div className="h-full w-full bg-[linear-gradient(90deg,rgba(16,16,20,0.68),transparent_58%),radial-gradient(circle_at_78%_30%,rgba(255,216,102,0.5),transparent_26%),radial-gradient(circle_at_60%_78%,rgba(255,97,136,0.38),transparent_24%)]" />
        </div>

        <SearchInput value={search} onChange={setSearch} placeholder="Search" className="mt-6" />
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
                  const unreadCount = unreadCounts[channel.id] ?? 0;

                  return (
                    <button
                      key={channel.id}
                      type="button"
                      onClick={() => onSelectChannel(channel.id)}
                      onContextMenu={(event) => {
                        if (!canManageChannels || !onOpenChannelPermissions) {
                          return;
                        }
                        event.preventDefault();
                        setChannelMenu({ channel, x: event.clientX, y: event.clientY });
                      }}
                      className={`mono-detail flex h-10 w-full items-center gap-3 rounded-md px-3 text-left text-[1rem] transition ${
                        isActive ? 'bg-frame text-white' : 'text-grey-link hover:bg-frame/60'
                      }`}
                    >
                      <Hash className="h-4 w-4 shrink-0 text-[#8a8a96]" strokeWidth={1.8} />
                      <span
                        className={`min-w-0 flex-1 truncate ${
                          isActive || unreadCount > 0 ? 'font-bold' : 'font-normal'
                        }`}
                      >
                        {channel.name}
                      </span>
                      {unreadCount > 0 ? (
                        <span className="mono-detail flex h-5 min-w-5 shrink-0 items-center justify-center rounded-full bg-pink px-1.5 text-[0.68rem] font-bold text-primary-bg">
                          {unreadCount}
                        </span>
                      ) : null}
                    </button>
                  );
                })}
              </div>
            </section>
          ))}
        </div>
      </div>

      <ConversationListFooter
        currentUser={currentUser}
        isMicMuted={isMicMuted}
        isDeafened={isDeafened}
        unreadNotifications={unreadNotifications}
        bellRef={bellRef}
        onToggleMicMute={onToggleMicMute}
        onToggleDeafen={onToggleDeafen}
        onOpenNotifications={onOpenNotifications}
        onOpenSettings={onOpenSettings}
      />

      {channelMenu && onOpenChannelPermissions ? (
        <ChannelContextMenu
          menu={channelMenu}
          onOpenPermissions={onOpenChannelPermissions}
          onClose={() => setChannelMenu(null)}
        />
      ) : null}
    </div>
  );
}
