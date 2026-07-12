'use client';

import { useCallback, useEffect, useRef, useState } from 'react';
import type { MouseEvent } from 'react';
import Link from 'next/link';
import { MessageCircle, Plus } from 'lucide-react';
import { AddGuildModal } from './guild/add-guild-modal';
import { GuildContextMenu, type GuildContextMenuTarget } from './guild/guild-context-menu';
import { GuildIcon } from './guild/guild-icon';
import { FortyTwoIcon } from './icons/brand-icons';
import { useGuilds } from '../shared/guilds/guild-store';
import { useToast } from '../shared/ui/toast';

type GuildSidebarProps = {
  activeMode: 'guild' | 'dm';
  onOpenDms: () => void;
  onOpenGuild: () => void;
};

// glow bars shown at the edges where the guild list is cropped, hinting that
// scrolling reveals more; a couple px of slack so they hide at the extremes
const SCROLL_EDGE_SLACK_PX = 4;

export function GuildSidebar({ activeMode, onOpenDms, onOpenGuild }: GuildSidebarProps) {
  const sidebarRef = useRef<HTMLElement>(null);
  const scrollRef = useRef<HTMLDivElement>(null);
  const { guilds, isLoading, error, selectedGuildId, selectGuild, refreshGuilds } = useGuilds();
  const [tooltip, setTooltip] = useState<{ name: string; top: number } | null>(null);
  const [contextMenu, setContextMenu] = useState<GuildContextMenuTarget | null>(null);
  const [isAddGuildOpen, setIsAddGuildOpen] = useState(false);
  const [croppedEdges, setCroppedEdges] = useState({ top: false, bottom: false });
  const { pushToast } = useToast();

  const updateCroppedEdges = useCallback(() => {
    const list = scrollRef.current;
    if (!list) {
      return;
    }

    const top = list.scrollTop > SCROLL_EDGE_SLACK_PX;
    const bottom = list.scrollTop + list.clientHeight < list.scrollHeight - SCROLL_EDGE_SLACK_PX;
    setCroppedEdges((previous) =>
      previous.top === top && previous.bottom === bottom ? previous : { top, bottom }
    );
  }, []);

  useEffect(() => {
    if (error) {
      pushToast({
        title: 'Guild list',
        description: error,
        tone: 'error'
      });
    }
  }, [error, pushToast]);

  useEffect(() => {
    updateCroppedEdges();

    const list = scrollRef.current;
    if (!list || typeof ResizeObserver === 'undefined') {
      return;
    }

    const observer = new ResizeObserver(updateCroppedEdges);
    observer.observe(list);
    return () => observer.disconnect();
    // re-measure when the list content changes, not just on resize
  }, [updateCroppedEdges, guilds.length, isLoading]);

  function handleSelectGuild(guildId: string) {
    selectGuild(guildId);
    onOpenGuild();
  }

  function handleOpenContextMenu(
    guild: { id: string; name: string },
    event: MouseEvent<HTMLButtonElement>
  ) {
    event.preventDefault();
    setTooltip(null);
    setContextMenu({ guildId: guild.id, guildName: guild.name, x: event.clientX, y: event.clientY });
  }

  function handleShowLabel(name: string, event: MouseEvent<HTMLButtonElement>) {
    const sidebarBox = sidebarRef.current?.getBoundingClientRect();
    const buttonBox = event.currentTarget.getBoundingClientRect();

    if (!sidebarBox) {
      return;
    }

    setTooltip({
      name,
      top: buttonBox.top - sidebarBox.top + buttonBox.height / 2
    });
  }

  return (
    <aside
      ref={sidebarRef}
      className="relative hidden min-h-0 w-[7.25rem] flex-col rounded-[1rem] bg-secondary-bg px-5 py-6 ring-1 ring-stroke md:flex"
    >
      <Link href="/" aria-label="Home" className="mx-auto block w-fit text-white">
        <FortyTwoIcon className="h-8 w-8" />
      </Link>
      <div className="mx-1 mt-5 border-t border-stroke" />
      <div className="relative flex min-h-0 flex-1 flex-col">
        <div
          ref={scrollRef}
          className="flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto pt-4"
          onScroll={() => {
            setTooltip(null);
            updateCroppedEdges();
          }}
        >
          <button
            type="button"
            onClick={onOpenDms}
            onMouseEnter={(event) => handleShowLabel('Direct Messages', event)}
            onMouseLeave={() => setTooltip(null)}
            className={`flex h-[4.9rem] shrink-0 items-center justify-center rounded-xl border transition ${
              activeMode === 'dm'
                ? 'border-aqua bg-panel text-aqua shadow-[0_0_0_1px_rgba(120,220,232,0.2)]'
                : 'border-frame bg-panel text-[#8b8b8f] hover:text-white'
            }`}
            aria-label="Direct messages"
          >
            <MessageCircle className="h-8 w-8" strokeWidth={1.7} />
          </button>
          <div className="mx-1 border-t border-stroke" />
          {isLoading && guilds.length === 0 ? (
            <div className="h-[4.9rem] shrink-0 animate-pulse rounded-xl border border-frame bg-panel" />
          ) : null}
          {error && guilds.length === 0 ? (
            <p className="px-1 text-center text-xs text-white/45" title={error}>
              Guilds unavailable
            </p>
          ) : null}
          {guilds.map((guild) => (
            <button
              key={guild.id}
              type="button"
              onClick={() => handleSelectGuild(guild.id)}
              onContextMenu={(event) => handleOpenContextMenu(guild, event)}
              onMouseEnter={(event) => handleShowLabel(guild.name, event)}
              onMouseLeave={() => setTooltip(null)}
              className={`h-[4.9rem] shrink-0 overflow-hidden rounded-xl border transition ${
                guild.id === selectedGuildId && activeMode === 'guild'
                  ? 'border-aqua shadow-[0_0_0_1px_rgba(120,220,232,0.2)]'
                  : 'border-frame'
              }`}
              aria-label={guild.name}
            >
              <GuildIcon
                guildId={guild.id}
                name={guild.name}
                iconUrl={guild.icon_url}
                className="h-full w-full text-2xl"
              />
            </button>
          ))}
          <button
            type="button"
            onClick={() => setIsAddGuildOpen(true)}
            onMouseEnter={(event) => handleShowLabel('Add a guild', event)}
            onMouseLeave={() => setTooltip(null)}
            className="flex h-[4.9rem] shrink-0 items-center justify-center rounded-xl bg-panel text-[#535353] transition hover:text-white"
            aria-label="Add guild"
          >
            <Plus className="h-8 w-8" strokeWidth={1.5} />
          </button>
        </div>
        <div
          aria-hidden
          className={`pointer-events-none absolute inset-x-0 top-0 h-px rounded-full bg-aqua shadow-[0_0_9px_2px_rgba(120,220,232,0.45)] transition-opacity duration-300 ${
            croppedEdges.top ? 'opacity-100' : 'opacity-0'
          }`}
        />
        <div
          aria-hidden
          className={`pointer-events-none absolute inset-x-0 bottom-0 h-px rounded-full bg-aqua shadow-[0_0_9px_2px_rgba(120,220,232,0.45)] transition-opacity duration-300 ${
            croppedEdges.bottom ? 'opacity-100' : 'opacity-0'
          }`}
        />
      </div>
      {tooltip ? (
        <div
          className="pointer-events-none absolute left-[calc(100%+0.4rem)] z-50 -translate-y-1/2 whitespace-nowrap rounded-md border border-stroke bg-panel px-4 py-2.5 text-base font-semibold text-white shadow-xl shadow-black/30"
          style={{ top: tooltip.top }}
        >
          {tooltip.name}
        </div>
      ) : null}
      {isAddGuildOpen ? <AddGuildModal onClose={() => setIsAddGuildOpen(false)} /> : null}
      {contextMenu ? (
        <GuildContextMenu
          target={contextMenu}
          onClose={() => setContextMenu(null)}
          onLeft={(guildId) => {
            if (guildId === selectedGuildId) {
              onOpenDms();
            }
            void refreshGuilds();
          }}
        />
      ) : null}
    </aside>
  );
}
