'use client';

import { useRef, useState } from 'react';
import type { MouseEvent } from 'react';
import Link from 'next/link';
import { MessageCircle, Plus } from 'lucide-react';
import { AddGuildModal } from './guild/add-guild-modal';
import { GuildIcon } from './guild/guild-icon';
import { FortyTwoIcon } from './icons/brand-icons';
import { useGuilds } from '../shared/guilds/guild-store';

type GuildSidebarProps = {
  activeMode: 'guild' | 'dm';
  onOpenDms: () => void;
  onOpenGuild: () => void;
};

export function GuildSidebar({ activeMode, onOpenDms, onOpenGuild }: GuildSidebarProps) {
  const sidebarRef = useRef<HTMLElement>(null);
  const { guilds, isLoading, error, selectedGuildId, selectGuild } = useGuilds();
  const [tooltip, setTooltip] = useState<{ name: string; top: number } | null>(null);
  const [isAddGuildOpen, setIsAddGuildOpen] = useState(false);

  function handleSelectGuild(guildId: string) {
    selectGuild(guildId);
    onOpenGuild();
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
      className="relative hidden min-h-0 w-[7.25rem] flex-col rounded-[1rem] bg-secondary-bg px-5 py-6 ring-1 ring-white/5 md:flex"
    >
      <Link href="/" aria-label="Home" className="mx-auto block w-fit text-white">
        <FortyTwoIcon className="h-8 w-8" />
      </Link>
      <div className="mx-1 mt-5 border-t border-white/10" />
      <div
        className="mt-6 flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto"
        onScroll={() => setTooltip(null)}
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
        <div className="mx-1 border-t border-white/10" />
        {isLoading && guilds.length === 0 ? (
          <div className="h-[4.9rem] shrink-0 animate-pulse rounded-xl border border-frame bg-panel" />
        ) : null}
        {error && guilds.length === 0 ? (
          <p className="px-1 text-center text-xs text-pink/80" title={error}>
            Guilds unavailable
          </p>
        ) : null}
        {guilds.map((guild) => (
          <button
            key={guild.id}
            type="button"
            onClick={() => handleSelectGuild(guild.id)}
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
      {tooltip ? (
        <div
          className="pointer-events-none absolute left-[calc(100%+0.4rem)] z-50 -translate-y-1/2 whitespace-nowrap rounded-md border border-white/10 bg-panel px-4 py-2.5 text-base font-semibold text-white shadow-xl shadow-black/30"
          style={{ top: tooltip.top }}
        >
          {tooltip.name}
        </div>
      ) : null}
      {isAddGuildOpen ? <AddGuildModal onClose={() => setIsAddGuildOpen(false)} /> : null}
    </aside>
  );
}
