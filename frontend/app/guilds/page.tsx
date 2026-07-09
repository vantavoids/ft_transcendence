'use client';

import { useState } from 'react';
import Link from 'next/link';
import { GuildIcon } from '../../src/components/guild/guild-icon';
import { GuildBansPanel } from '../../src/components/guild/guild-bans-panel';
import { GuildCategoriesPanel } from '../../src/components/guild/guild-categories-panel';
import { GuildCreateForm, GuildJoinForm } from '../../src/components/guild/guild-forms';
import { GuildInvitesPanel } from '../../src/components/guild/guild-invites-panel';
import { GuildMembersPanel } from '../../src/components/guild/guild-members-panel';
import { GuildOverview } from '../../src/components/guild/guild-overview';
import { GuildRolesPanel } from '../../src/components/guild/guild-roles-panel';
import { GuildSettingsPanel } from '../../src/components/guild/guild-settings-panel';
import { useGuilds } from '../../src/shared/guilds/guild-store';

const TABS = [
  { id: 'overview', label: 'Overview' },
  { id: 'members', label: 'Members' },
  { id: 'bans', label: 'Bans' },
  { id: 'invites', label: 'Invites' },
  { id: 'roles', label: 'Roles' },
  { id: 'categories', label: 'Categories' },
  { id: 'settings', label: 'Settings' }
] as const;

type TabId = (typeof TABS)[number]['id'];

export default function GuildsPage() {
  const { guilds, isLoading, error, selectedGuildId, selectGuild } = useGuilds();
  const [activeTab, setActiveTab] = useState<TabId>('overview');

  return (
    <section className="mx-auto flex min-h-screen w-full max-w-5xl flex-col px-6 py-10">
      <div className="w-full rounded-[2rem] border border-white/8 bg-secondary-bg/90 p-8 shadow-2xl shadow-black/40 md:p-10">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div>
            <p className="mono-detail text-aqua">Guilds</p>
            <h1 className="mt-2 text-4xl font-extrabold tracking-[-0.07em] text-white md:text-5xl">
              Your guilds
            </h1>
          </div>
          <Link
            href="/chat"
            className="rounded-full border border-aqua/50 bg-aqua/10 px-5 py-3 font-semibold text-aqua transition hover:bg-aqua/20"
          >
            Ouvrir le chat
          </Link>
        </div>

        {error && guilds.length === 0 ? (
          <p className="mt-6 rounded-md border border-pink/25 bg-pink/10 px-4 py-3 text-sm text-pink">
            {error}
          </p>
        ) : null}

        {guilds.length > 0 ? (
          <div className="mt-6 flex flex-wrap gap-3">
            {guilds.map((guild) => (
              <button
                key={guild.id}
                type="button"
                onClick={() => selectGuild(guild.id)}
                className={`flex items-center gap-2 rounded-full border py-1.5 pl-1.5 pr-4 text-sm font-semibold transition ${
                  guild.id === selectedGuildId
                    ? 'border-aqua/60 bg-aqua/10 text-aqua'
                    : 'border-white/10 bg-panel text-white/60 hover:text-white'
                }`}
              >
                <GuildIcon
                  guildId={guild.id}
                  name={guild.name}
                  iconUrl={guild.icon_url}
                  className="h-7 w-7 overflow-hidden rounded-full text-xs"
                />
                {guild.name}
              </button>
            ))}
          </div>
        ) : null}

        <div className="mt-8">
          {selectedGuildId ? (
            <>
              <div className="mb-5 flex flex-wrap gap-2">
                {TABS.map((tab) => (
                  <button
                    key={tab.id}
                    type="button"
                    onClick={() => setActiveTab(tab.id)}
                    className={`h-9 rounded-md px-4 text-sm font-bold transition ${
                      activeTab === tab.id
                        ? 'bg-aqua/15 text-aqua'
                        : 'bg-panel text-white/45 hover:text-white'
                    }`}
                  >
                    {tab.label}
                  </button>
                ))}
              </div>
              {activeTab === 'overview' ? <GuildOverview guildId={selectedGuildId} /> : null}
              {activeTab === 'members' ? <GuildMembersPanel guildId={selectedGuildId} /> : null}
              {activeTab === 'bans' ? <GuildBansPanel guildId={selectedGuildId} /> : null}
              {activeTab === 'invites' ? <GuildInvitesPanel guildId={selectedGuildId} /> : null}
              {activeTab === 'roles' ? <GuildRolesPanel guildId={selectedGuildId} /> : null}
              {activeTab === 'categories' ? (
                <GuildCategoriesPanel guildId={selectedGuildId} />
              ) : null}
              {activeTab === 'settings' ? <GuildSettingsPanel guildId={selectedGuildId} /> : null}
            </>
          ) : isLoading ? (
            <div className="h-48 animate-pulse rounded-[1rem] bg-panel" />
          ) : (
            <p className="max-w-2xl text-lg text-white/65">
              Tu n&apos;as pas encore de guilde. Crée la tienne ou rejoins-en une avec un code
              d&apos;invitation.
            </p>
          )}
        </div>

        <div className="mt-10 grid gap-8 md:grid-cols-2">
          <div className="rounded-[1rem] border border-white/8 bg-panel p-6">
            <h2 className="text-xl font-bold tracking-[-0.03em] text-white">Create a guild</h2>
            <p className="mt-1 text-sm text-white/45">
              Démarre une nouvelle guilde dont tu seras propriétaire.
            </p>
            <div className="mt-5">
              <GuildCreateForm />
            </div>
          </div>
          <div className="rounded-[1rem] border border-white/8 bg-panel p-6">
            <h2 className="text-xl font-bold tracking-[-0.03em] text-white">Join a guild</h2>
            <p className="mt-1 text-sm text-white/45">
              Utilise un code d&apos;invitation pour rejoindre une guilde existante.
            </p>
            <div className="mt-5">
              <GuildJoinForm />
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
