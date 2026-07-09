'use client';

import { useEffect, useState } from 'react';
import { Crown, Users } from 'lucide-react';
import { getGuild, type GuildDto } from '../../shared/api/guild';
import { useGuilds } from '../../shared/guilds/guild-store';
import { GuildIcon } from './guild-icon';

export function formatDate(value: string | null | undefined) {
  if (!value) {
    return '—';
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? '—'
    : date.toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' });
}

type GuildOverviewProps = {
  guildId: string;
};

export function GuildOverview({ guildId }: GuildOverviewProps) {
  const { currentUserId } = useGuilds();
  const [guild, setGuild] = useState<GuildDto | null>(null);
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let isCancelled = false;

    setIsLoading(true);
    setError('');
    setGuild(null);

    getGuild(guildId)
      .then((details) => {
        if (!isCancelled) {
          setGuild(details);
        }
      })
      .catch((loadError) => {
        if (!isCancelled) {
          setError(loadError instanceof Error ? loadError.message : 'Failed to load guild.');
        }
      })
      .finally(() => {
        if (!isCancelled) {
          setIsLoading(false);
        }
      });

    return () => {
      isCancelled = true;
    };
  }, [guildId]);

  if (isLoading) {
    return <div className="h-48 animate-pulse rounded-[1rem] bg-panel" />;
  }

  if (error || !guild) {
    return (
      <p className="rounded-md border border-pink/25 bg-pink/10 px-4 py-3 text-sm text-pink">
        {error || 'Guild not found.'}
      </p>
    );
  }

  return (
    <div className="overflow-hidden rounded-[1rem] border border-white/8 bg-panel">
      <div
        className="h-28 bg-[linear-gradient(135deg,#232329_0%,#2b3141_38%,#24545b_68%,#78dce8_100%)] bg-cover bg-center"
        style={guild.banner_url ? { backgroundImage: `url(${guild.banner_url})` } : undefined}
      />
      <div className="px-6 pb-6">
        <div className="-mt-8 flex items-end gap-4">
          <GuildIcon
            guildId={guild.id}
            name={guild.name}
            iconUrl={guild.icon_url}
            className="h-20 w-20 overflow-hidden rounded-2xl border-4 border-panel text-3xl"
          />
          <div className="min-w-0 pb-1">
            <h2 className="flex items-center gap-2 truncate text-3xl font-extrabold tracking-[-0.05em] text-white">
              {guild.name}
              {currentUserId && guild.owner_id === currentUserId ? (
                <Crown
                  className="h-5 w-5 shrink-0 text-yellow"
                  strokeWidth={1.9}
                  aria-label="You own this guild"
                />
              ) : null}
            </h2>
          </div>
        </div>
        {guild.description ? (
          <p className="mt-4 max-w-2xl text-base text-white/65">{guild.description}</p>
        ) : null}
        <div className="mt-5 flex flex-wrap gap-x-6 gap-y-2 text-sm text-white/45">
          <span className="inline-flex items-center gap-2">
            <Users className="h-4 w-4" strokeWidth={1.8} />
            {guild.member_count ?? '—'} member{(guild.member_count ?? 0) === 1 ? '' : 's'}
          </span>
          <span>Created {formatDate(guild.created_at)}</span>
          <span className="mono-detail">id {guild.id}</span>
        </div>
      </div>
    </div>
  );
}
