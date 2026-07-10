'use client';

import { useCallback, useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Check, Copy, Link2, Search } from 'lucide-react';
import {
  createGuildInvite,
  listGuildInvites,
  previewInvite,
  revokeGuildInvite,
  type GuildInviteDto,
  type InvitePreviewDto
} from '../../shared/api/guild';
import { getUsersByIds, type UserSummaryDto } from '../../shared/api/user';
import { formatDate } from './guild-overview';
import { GuildIcon } from './guild-icon';
import { FormError } from './guild-forms';

const inputClasses =
  'h-11 w-full rounded-md border border-transparent bg-input-bg px-4 text-base text-white outline-none transition placeholder:text-input-placeholder focus:border-aqua/35';

type GuildInvitesPanelProps = {
  guildId: string;
};

export function GuildInvitesPanel({ guildId }: GuildInvitesPanelProps) {
  const [invites, setInvites] = useState<GuildInviteDto[]>([]);
  const [usersById, setUsersById] = useState<Map<string, UserSummaryDto>>(new Map());
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [maxUses, setMaxUses] = useState('');
  const [expiresInHours, setExpiresInHours] = useState('');
  const [isCreating, setIsCreating] = useState(false);
  const [createdCode, setCreatedCode] = useState('');
  const [previewCode, setPreviewCode] = useState('');
  const [preview, setPreview] = useState<InvitePreviewDto | null>(null);
  const [previewError, setPreviewError] = useState('');
  const [isPreviewing, setIsPreviewing] = useState(false);
  const [copiedCode, setCopiedCode] = useState('');

  async function handleCopyCode(code: string) {
    try {
      await navigator.clipboard.writeText(code);
      setCopiedCode(code);
      window.setTimeout(() => setCopiedCode((current) => (current === code ? '' : current)), 1500);
    } catch {
      // best effort: clipboard access can be denied, nothing to recover from
    }
  }

  const load = useCallback(async () => {
    setIsLoading(true);
    setError('');

    try {
      const rows = await listGuildInvites(guildId);
      setInvites(rows);

      const users = await getUsersByIds(rows.map((invite) => invite.created_by));
      setUsersById(new Map(users.map((user) => [user.id, user])));
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : 'Failed to load invites.');
    } finally {
      setIsLoading(false);
    }
  }, [guildId]);

  useEffect(() => {
    void load();
  }, [load]);

  async function handleCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError('');
    setCreatedCode('');

    const parsedMaxUses = maxUses.trim() ? Number(maxUses) : undefined;
    const parsedExpires = expiresInHours.trim() ? Number(expiresInHours) : undefined;

    if (
      (parsedMaxUses !== undefined && (!Number.isInteger(parsedMaxUses) || parsedMaxUses <= 0)) ||
      (parsedExpires !== undefined && (!Number.isInteger(parsedExpires) || parsedExpires <= 0))
    ) {
      setError('Max uses and expiry must be positive whole numbers.');
      return;
    }

    try {
      setIsCreating(true);
      const invite = await createGuildInvite(guildId, {
        max_uses: parsedMaxUses,
        expires_in_hours: parsedExpires
      });
      setCreatedCode(invite.code);
      setMaxUses('');
      setExpiresInHours('');
      await load();
    } catch (createError) {
      setError(createError instanceof Error ? createError.message : 'Failed to create invite.');
    } finally {
      setIsCreating(false);
    }
  }

  async function handleRevoke(code: string) {
    if (!window.confirm(`Revoke invite ${code}? It becomes unusable immediately.`)) {
      return;
    }

    setError('');

    try {
      await revokeGuildInvite(guildId, code);
      await load();
    } catch (revokeError) {
      setError(revokeError instanceof Error ? revokeError.message : 'Failed to revoke invite.');
    }
  }

  async function handlePreview(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPreviewError('');
    setPreview(null);

    const code = previewCode.trim();
    if (!code) {
      setPreviewError('Enter an invite code to preview.');
      return;
    }

    try {
      setIsPreviewing(true);
      setPreview(await previewInvite(code));
    } catch (lookupError) {
      setPreviewError(
        lookupError instanceof Error ? lookupError.message : 'Invite not found or expired.'
      );
    } finally {
      setIsPreviewing(false);
    }
  }

  function describeCreator(userId: string) {
    const user = usersById.get(userId);
    return user ? `@${user.username}` : `user ${userId}`;
  }

  return (
    <div className="grid gap-5">
      <form
        onSubmit={handleCreate}
        className="grid gap-3 rounded-md border border-white/8 bg-panel p-4"
      >
        <h3 className="flex items-center gap-2 text-base font-bold text-white">
          <Link2 className="h-4 w-4 text-aqua" strokeWidth={1.9} />
          Create an invite
        </h3>
        <div className="grid gap-3 sm:grid-cols-2">
          <input
            value={maxUses}
            onChange={(event) => setMaxUses(event.target.value)}
            placeholder="max uses (unlimited if empty)"
            inputMode="numeric"
            className={inputClasses}
          />
          <input
            value={expiresInHours}
            onChange={(event) => setExpiresInHours(event.target.value)}
            placeholder="expires in hours (never if empty)"
            inputMode="numeric"
            className={inputClasses}
          />
        </div>
        <div className="flex flex-wrap items-center gap-3">
          <button
            type="submit"
            disabled={isCreating}
            className="h-10 rounded-md bg-aqua px-5 text-sm font-bold text-primary-bg transition hover:bg-white disabled:cursor-not-allowed disabled:bg-frame disabled:text-white/25"
          >
            {isCreating ? 'Creating...' : 'Create invite'}
          </button>
          {createdCode ? (
            <span className="mono-detail flex items-center gap-2 rounded-md border border-lime/30 bg-lime/10 px-3 py-2 text-sm text-lime">
              Invite created: {createdCode}
              <button
                type="button"
                onClick={() => void handleCopyCode(createdCode)}
                className="text-lime/70 transition hover:text-lime"
                aria-label="Copy invite code"
              >
                {copiedCode === createdCode ? (
                  <Check className="h-3.5 w-3.5" strokeWidth={2} />
                ) : (
                  <Copy className="h-3.5 w-3.5" strokeWidth={1.9} />
                )}
              </button>
            </span>
          ) : null}
        </div>
      </form>

      {error ? <FormError message={error} /> : null}

      {isLoading ? (
        <div className="h-24 animate-pulse rounded-md bg-panel" />
      ) : invites.length === 0 ? (
        <p className="text-sm text-white/35">No active invites.</p>
      ) : (
        <ul className="grid gap-2">
          {invites.map((invite) => (
            <li
              key={invite.code}
              className="flex items-center gap-3 rounded-md border border-white/8 bg-panel px-3 py-2.5"
            >
              <div className="min-w-0 flex-1">
                <p className="mono-detail flex items-center gap-1.5 truncate text-[0.95rem] font-bold text-white">
                  {invite.code}
                  <button
                    type="button"
                    onClick={() => void handleCopyCode(invite.code)}
                    className="shrink-0 text-white/35 transition hover:text-white"
                    aria-label={`Copy invite code ${invite.code}`}
                  >
                    {copiedCode === invite.code ? (
                      <Check className="h-3.5 w-3.5 text-lime" strokeWidth={2} />
                    ) : (
                      <Copy className="h-3.5 w-3.5" strokeWidth={1.9} />
                    )}
                  </button>
                </p>
                <p className="truncate text-xs text-white/35">
                  {invite.uses}/{invite.max_uses ?? '∞'} uses · by{' '}
                  {describeCreator(invite.created_by)} ·{' '}
                  {invite.expires_at ? `expires ${formatDate(invite.expires_at)}` : 'never expires'}
                </p>
              </div>
              <button
                type="button"
                onClick={() => void handleRevoke(invite.code)}
                className="h-8 shrink-0 rounded-md border border-white/10 px-3 text-xs font-bold text-white/60 transition hover:border-pink/40 hover:text-pink"
              >
                Revoke
              </button>
            </li>
          ))}
        </ul>
      )}

      <form
        onSubmit={handlePreview}
        className="grid gap-3 rounded-md border border-white/8 bg-panel p-4"
      >
        <h3 className="flex items-center gap-2 text-base font-bold text-white">
          <Search className="h-4 w-4 text-lavender" strokeWidth={1.9} />
          Preview an invite
        </h3>
        <div className="flex gap-3">
          <input
            value={previewCode}
            onChange={(event) => setPreviewCode(event.target.value)}
            placeholder="invite code"
            className={inputClasses}
          />
          <button
            type="submit"
            disabled={isPreviewing}
            className="h-11 shrink-0 rounded-md border border-white/10 px-5 text-sm font-bold text-white/70 transition hover:border-aqua/40 hover:text-aqua disabled:cursor-not-allowed disabled:opacity-50"
          >
            {isPreviewing ? 'Looking up...' : 'Preview'}
          </button>
        </div>
        {previewError ? <FormError message={previewError} /> : null}
        {preview ? (
          <div className="flex items-center gap-3 rounded-md border border-white/10 bg-frame/50 px-3 py-2.5">
            <GuildIcon
              guildId={preview.guild.id}
              name={preview.guild.name}
              iconUrl={preview.guild.icon_url}
              className="h-10 w-10 overflow-hidden rounded-xl text-sm"
            />
            <div className="min-w-0">
              <p className="truncate text-[0.95rem] font-bold text-white">{preview.guild.name}</p>
              <p className="truncate text-xs text-white/35">
                {preview.guild.member_count ?? '—'} members · invited by @{preview.inviter.username}{' '}
                ·{' '}
                {preview.expires_at ? `expires ${formatDate(preview.expires_at)}` : 'never expires'}
              </p>
            </div>
          </div>
        ) : null}
      </form>
    </div>
  );
}
