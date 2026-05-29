'use client';

import { useRef, useState } from 'react';
import type { MouseEvent } from 'react';
import Image from 'next/image';
import Link from 'next/link';
import { MessageCircle, Plus } from 'lucide-react';

type Guild = {
  id: string;
  name: string;
  iconUrl: string;
};

type GuildSidebarProps = {
  activeMode: 'guild' | 'dm';
  onOpenDms: () => void;
  onOpenGuild: () => void;
};

const initialGuilds: Guild[] = [
  {
    id: 'default',
    name: 'Default guild',
    iconUrl: 'https://placehold.co/160x160/png?text=G'
  }
];

const guildNames = ['Neon Arena', 'Pixel Club', 'Pong Squad', 'Byte House', 'Arcade Hub'];
const guildColors = ['78dce8', 'a9dc76', 'ffd866', 'ff6188', 'ab9df2'];

function createRandomGuild(): Guild {
  const name = guildNames[Math.floor(Math.random() * guildNames.length)];
  const color = guildColors[Math.floor(Math.random() * guildColors.length)];
  const text = encodeURIComponent(name.slice(0, 1));

  return {
    id: `${name.toLowerCase().replace(/\s+/g, '-')}-${Date.now()}`,
    name,
    iconUrl: `https://placehold.co/160x160/${color}/101014/png?text=${text}`
  };
}

export function GuildSidebar({ activeMode, onOpenDms, onOpenGuild }: GuildSidebarProps) {
  const sidebarRef = useRef<HTMLElement>(null);
  const [guilds, setGuilds] = useState(initialGuilds);
  const [tooltip, setTooltip] = useState<{ name: string; top: number } | null>(null);

  function handleAddGuild() {
    setGuilds((current) => [...current, createRandomGuild()]);
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
      <Link href="/" className="mono-detail text-[2rem] font-bold tracking-[-0.06em] text-white">
        Logo<span className="text-aqua">_</span>
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
        {guilds.map((guild, index) => (
          <button
            key={guild.id}
            type="button"
            onClick={onOpenGuild}
            onMouseEnter={(event) => handleShowLabel(guild.name, event)}
            onMouseLeave={() => setTooltip(null)}
            className={`h-[4.9rem] shrink-0 overflow-hidden rounded-xl border transition ${
              index === 0 && activeMode === 'guild'
                ? 'border-aqua shadow-[0_0_0_1px_rgba(120,220,232,0.2)]'
                : 'border-frame'
            }`}
            aria-label={guild.name}
          >
            <Image
              src={guild.iconUrl}
              alt=""
              width={160}
              height={160}
              className="h-full w-full object-cover"
            />
          </button>
        ))}
        <button
          type="button"
          onClick={handleAddGuild}
          className="flex h-[4.9rem] shrink-0 items-center justify-center rounded-xl bg-panel text-[#535353] transition hover:text-white"
          aria-label="Add server"
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
      <div className="flex justify-center pt-5 text-3xl tracking-[0.4em] text-[#9f9f9f]">...</div>
    </aside>
  );
}
