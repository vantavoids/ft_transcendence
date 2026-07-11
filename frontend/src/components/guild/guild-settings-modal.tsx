'use client';

import { useState } from 'react';
import {
  Banknote,
  Boxes,
  CircleEllipsis,
  Columns3,
  FilePlus2,
  Gavel,
  LayoutGrid,
  Settings,
  UserPlus,
  Users,
  X
} from 'lucide-react';
import { useCloseOnEscape } from '../../shared/hooks/use-close-on-escape';
import { useGuilds } from '../../shared/guilds/guild-store';
import { GuildCreateForm, GuildJoinForm } from './guild-forms';
import { GuildOverview } from './guild-overview';
import { GuildMembersPanel } from './guild-members-panel';
import { GuildBansPanel } from './guild-bans-panel';
import { GuildInvitesPanel } from './guild-invites-panel';
import { GuildRolesPanel } from './guild-roles-panel';
import { GuildCategoriesPanel } from './guild-categories-panel';
import { GuildChannelsPanel } from './guild-channels-panel';
import { GuildSettingsPanel } from './guild-settings-panel';
import { GuildIcon } from './guild-icon';

type GuildSettingsModalProps = {
  guildId: string;
  onClose: () => void;
};

type TabId =
  | 'overview'
  | 'members'
  | 'bans'
  | 'invites'
  | 'roles'
  | 'categories'
  | 'channels'
  | 'settings'
  | 'create'
  | 'join';

type TabItem = {
  id: TabId;
  label: string;
  icon: typeof LayoutGrid;
};

const MAIN_TABS: TabItem[] = [
  { id: 'overview', label: 'Overview', icon: LayoutGrid },
  { id: 'members', label: 'Members', icon: Users },
  { id: 'bans', label: 'Bans', icon: Gavel },
  { id: 'invites', label: 'Invites', icon: CircleEllipsis },
  { id: 'roles', label: 'Roles', icon: Banknote },
  { id: 'categories', label: 'Categories', icon: Boxes },
  { id: 'channels', label: 'Channels', icon: Columns3 },
  { id: 'settings', label: 'Settings', icon: Settings }
];

const ACTION_TABS: TabItem[] = [
  { id: 'create', label: 'Create guild', icon: FilePlus2 },
  { id: 'join', label: 'Join guild', icon: UserPlus }
];

function SidebarButton({
  active,
  icon: Icon,
  label,
  onClick
}: {
  active: boolean;
  icon: TabItem['icon'];
  label: string;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`flex h-10 w-full items-center gap-3 rounded-md px-3 text-left text-sm font-semibold transition ${
        active ? 'bg-aqua/15 text-aqua' : 'text-white/50 hover:bg-panel hover:text-white'
      }`}
    >
      <Icon className="h-4 w-4 shrink-0" strokeWidth={1.9} />
      <span className="truncate">{label}</span>
    </button>
  );
}

export function GuildSettingsModal({ guildId, onClose }: GuildSettingsModalProps) {
  const { selectedGuild } = useGuilds();
  const [tab, setTab] = useState<TabId>('overview');

  useCloseOnEscape(onClose);

  function renderContent() {
    switch (tab) {
      case 'overview':
        return <GuildOverview guildId={guildId} />;
      case 'members':
        return <GuildMembersPanel guildId={guildId} />;
      case 'bans':
        return <GuildBansPanel guildId={guildId} />;
      case 'invites':
        return <GuildInvitesPanel guildId={guildId} />;
      case 'roles':
        return <GuildRolesPanel guildId={guildId} />;
      case 'categories':
        return <GuildCategoriesPanel guildId={guildId} />;
      case 'channels':
        return <GuildChannelsPanel guildId={guildId} />;
      case 'settings':
        return <GuildSettingsPanel guildId={guildId} />;
      case 'create':
        return (
          <div className="rounded-[1rem] border border-white/8 bg-panel p-5">
            <h3 className="text-xl font-bold tracking-[-0.03em] text-white">Create a guild</h3>
            <p className="mt-1 text-sm text-white/45">
              Start a new guild without leaving the chat workspace.
            </p>
            <div className="mt-5">
              <GuildCreateForm onCreated={onClose} />
            </div>
          </div>
        );
      case 'join':
        return (
          <div className="rounded-[1rem] border border-white/8 bg-panel p-5">
            <h3 className="text-xl font-bold tracking-[-0.03em] text-white">Join a guild</h3>
            <p className="mt-1 text-sm text-white/45">
              Use an invite code to join an existing guild.
            </p>
            <div className="mt-5">
              <GuildJoinForm onJoined={onClose} />
            </div>
          </div>
        );
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/55 px-4 py-6">
      <button
        type="button"
        className="absolute inset-0 cursor-default"
        onClick={onClose}
        aria-label="Close guild manager"
      />
      <section className="relative w-full max-w-[86rem] overflow-hidden rounded-[1rem] bg-secondary-bg shadow-2xl shadow-black/50 ring-1 ring-white/10">
        <div className="flex h-[4.75rem] items-center justify-between border-b border-white/8 px-5">
          <div className="flex min-w-0 items-center gap-3">
            <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-md bg-aqua/10 text-aqua">
              <Settings className="h-5 w-5" strokeWidth={1.9} />
            </span>
            <div className="min-w-0">
              <h2 className="truncate text-[1.15rem] font-bold tracking-[-0.03em] text-white">
                {selectedGuild?.name ?? 'Guild manager'}
              </h2>
              <p className="font-category text-[0.7rem] uppercase tracking-[0.14em] text-white/35">
                Guild management
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="flex h-9 w-9 items-center justify-center rounded-md text-[#8b8b8f] transition hover:bg-frame hover:text-white"
            aria-label="Close guild manager"
          >
            <X className="h-4 w-4" strokeWidth={2} />
          </button>
        </div>

        <div className="grid max-h-[calc(100vh-7rem)] min-h-0 lg:grid-cols-[18rem_minmax(0,1fr)]">
          <aside className="border-b border-white/8 p-5 lg:min-h-0 lg:border-b-0 lg:border-r">
            <div className="rounded-[1rem] border border-white/8 bg-panel p-4">
              <div className="flex items-center gap-3">
                <GuildIcon
                  guildId={guildId}
                  name={selectedGuild?.name ?? 'Guild'}
                  iconUrl={selectedGuild?.icon_url}
                  className="h-12 w-12 shrink-0 overflow-hidden rounded-2xl text-xl"
                />
                <div className="min-w-0">
                  <p className="truncate text-base font-bold text-white">
                    {selectedGuild?.name ?? 'Guild'}
                  </p>
                  <p className="truncate text-xs text-white/40">
                    {selectedGuild ? `${selectedGuild.member_count} members` : 'Loading guild'}
                  </p>
                </div>
              </div>
            </div>

            <div className="mt-5 grid gap-5">
              <div>
                <p className="font-category px-1 text-[0.68rem] uppercase tracking-[0.14em] text-white/30">
                  Manage guild
                </p>
                <div className="mt-2 grid gap-1.5">
                  {MAIN_TABS.map((item) => (
                    <SidebarButton
                      key={item.id}
                      active={tab === item.id}
                      icon={item.icon}
                      label={item.label}
                      onClick={() => setTab(item.id)}
                    />
                  ))}
                </div>
              </div>

              <div>
                <p className="font-category px-1 text-[0.68rem] uppercase tracking-[0.14em] text-white/30">
                  Quick actions
                </p>
                <div className="mt-2 grid gap-1.5">
                  {ACTION_TABS.map((item) => (
                    <SidebarButton
                      key={item.id}
                      active={tab === item.id}
                      icon={item.icon}
                      label={item.label}
                      onClick={() => setTab(item.id)}
                    />
                  ))}
                </div>
              </div>
            </div>
          </aside>

          <div className="min-h-0 min-w-0 overflow-y-auto p-5 md:p-6">
            {renderContent()}
          </div>
        </div>
      </section>
    </div>
  );
}
