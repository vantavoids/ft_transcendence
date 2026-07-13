'use client';

import { useCallback, useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Gavel, ShieldOff } from 'lucide-react';
import { ApiError } from '../../shared/api/client';
import {
  banGuildMember,
  listGuildBans,
  unbanGuildMember,
  type GuildBanDto
} from '../../shared/api/guild';
import { getUsersByIds, type UserSummaryDto } from '../../shared/api/user';
import { ActionModal } from '../action-modal';
import { formatDate } from './guild-overview';
import { useToast } from '../../shared/ui/toast';

const inputClasses =
  'h-11 w-full rounded-md border border-transparent bg-input-bg px-4 text-base text-white outline-none transition placeholder:text-input-placeholder focus:border-aqua/35';

type GuildBansPanelProps = {
  guildId: string;
};

export function GuildBansPanel({ guildId }: GuildBansPanelProps) {
  const [bans, setBans] = useState<GuildBanDto[]>([]);
  const [usersById, setUsersById] = useState<Map<string, UserSummaryDto>>(new Map());
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [isForbidden, setIsForbidden] = useState(false);
  const [banUserId, setBanUserId] = useState('');
  const [banReason, setBanReason] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [unbanTarget, setUnbanTarget] = useState<string | null>(null);
  const [isUnbanning, setIsUnbanning] = useState(false);
  const { pushToast } = useToast();

  const load = useCallback(async () => {
    setIsLoading(true);
    setError('');
    setIsForbidden(false);

    try {
      const rows = await listGuildBans(guildId, { limit: 100 });
      setBans(rows);

      const ids = rows
        .flatMap((ban) => [ban.user_id, ban.banned_by])
        .filter((id): id is string => Boolean(id));
      const users = await getUsersByIds(ids);
      setUsersById(new Map(users.map((user) => [user.id, user])));
    } catch (loadError) {
      // Missing BAN_MEMBERS is an expected permission state, not an error to
      // alert on - show a calm inline notice instead of an action-error toast.
      if (loadError instanceof ApiError && loadError.status === 403) {
        setIsForbidden(true);
      } else {
        setError(loadError instanceof Error ? loadError.message : 'Failed to load bans.');
      }
    } finally {
      setIsLoading(false);
    }
  }, [guildId]);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    if (error) {
      pushToast({
        title: 'Bans',
        description: error,
        tone: 'error'
      });
    }
  }, [error, pushToast]);

  function describeUser(userId: string | null) {
    if (!userId) {
      return 'deleted moderator';
    }

    const user = usersById.get(userId);
    return user ? `${user.display_name} (@${user.username})` : `user ${userId}`;
  }

  async function handleBan(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError('');

    const userId = banUserId.trim();
    if (!userId) {
      setError('A user id is required to create a ban.');
      return;
    }

    try {
      setIsSubmitting(true);
      await banGuildMember(guildId, userId, banReason.trim() || undefined);
      setBanUserId('');
      setBanReason('');
      await load();
    } catch (banError) {
      setError(banError instanceof Error ? banError.message : 'Failed to ban user.');
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleUnban(userId: string) {
    setUnbanTarget(userId);
  }

  async function confirmUnban() {
    if (!unbanTarget) {
      return;
    }

    setError('');

    try {
      setIsUnbanning(true);
      await unbanGuildMember(guildId, unbanTarget);
      await load();
      setUnbanTarget(null);
    } catch (unbanError) {
      setError(unbanError instanceof Error ? unbanError.message : 'Failed to unban user.');
    } finally {
      setIsUnbanning(false);
    }
  }

  if (isForbidden) {
    return (
      <div className="flex flex-col items-center gap-3 rounded-md border border-stroke bg-panel px-5 py-10 text-center">
        <ShieldOff className="h-8 w-8 text-white/25" strokeWidth={1.6} />
        <p className="text-sm font-bold text-white/70">You don&apos;t have permission to manage bans</p>
        <p className="max-w-[22rem] text-xs text-white/35">
          Ask someone with the <span className="font-semibold text-white/50">Ban Members</span>{' '}
          permission in this guild to make changes here.
        </p>
      </div>
    );
  }

  return (
    <div className="grid gap-5">
      <form
        onSubmit={handleBan}
        className="grid gap-3 rounded-md border border-stroke bg-panel p-4"
      >
        <h3 className="flex items-center gap-2 text-base font-bold text-white">
          <Gavel className="h-4 w-4 text-pink" strokeWidth={1.9} />
          Ban a user
        </h3>
        <div className="grid gap-3 sm:grid-cols-2">
          <input
            value={banUserId}
            onChange={(event) => setBanUserId(event.target.value)}
            placeholder="user id"
            className={inputClasses}
          />
          <input
            value={banReason}
            onChange={(event) => setBanReason(event.target.value)}
            placeholder="reason (optional)"
            maxLength={512}
            className={inputClasses}
          />
        </div>
        <button
          type="submit"
          disabled={isSubmitting}
          className="h-10 w-fit rounded-md border border-pink/25 bg-pink/10 px-5 text-sm font-bold text-pink transition hover:border-pink/45 hover:bg-pink/15 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {isSubmitting ? 'Banning...' : 'Ban user'}
        </button>
      </form>

      {isLoading ? (
        <div className="h-24 animate-pulse rounded-md bg-panel" />
      ) : bans.length === 0 ? (
        <p className="text-sm text-white/35">No banned users.</p>
      ) : (
        <ul className="grid gap-2">
          {bans.map((ban) => (
            <li
              key={ban.user_id}
              className="flex items-center gap-3 rounded-md border border-stroke bg-panel px-3 py-2.5"
            >
              <div className="min-w-0 flex-1">
                <p className="truncate text-[0.95rem] font-bold text-white">
                  {describeUser(ban.user_id)}
                </p>
                <p className="truncate text-xs text-white/35">
                  {ban.reason ? `"${ban.reason}" · ` : ''}
                  banned by {describeUser(ban.banned_by)} · {formatDate(ban.banned_at)}
                </p>
              </div>
              <button
                type="button"
                onClick={() => void handleUnban(ban.user_id)}
                className="h-8 shrink-0 rounded-md border border-stroke px-3 text-xs font-bold text-white/60 transition hover:border-aqua/40 hover:text-aqua"
              >
                Unban
              </button>
            </li>
          ))}
        </ul>
      )}

      {unbanTarget ? (
        <ActionModal
          title={`Unban ${describeUser(unbanTarget)}?`}
          description="This will restore the user's access to the guild immediately."
          confirmLabel="Unban user"
          isBusy={isUnbanning}
          onClose={() => setUnbanTarget(null)}
          onConfirm={confirmUnban}
        />
      ) : null}
    </div>
  );
}
