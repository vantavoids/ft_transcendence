'use client';

import { useState } from 'react';
import type { FormEvent } from 'react';
import { Plus } from 'lucide-react';
import type { GuildDto } from '../../shared/api/guild';
import { useGuilds } from '../../shared/guilds/guild-store';

const inputClasses =
  'h-11 w-full rounded-md border border-transparent bg-input-bg px-4 text-base text-white outline-none transition placeholder:text-input-placeholder focus:border-aqua/35';

const submitClasses =
  'flex h-11 items-center justify-center gap-2 rounded-md bg-aqua px-5 text-sm font-bold text-primary-bg transition hover:bg-white disabled:cursor-not-allowed disabled:bg-frame disabled:text-white/25';

export function FormError({ message }: { message: string }) {
  return (
    <p className="rounded-md border border-pink/25 bg-pink/10 px-3 py-2 text-sm text-pink">
      {message}
    </p>
  );
}

type GuildCreateFormProps = {
  onCreated?: (guild: GuildDto) => void;
};

export function GuildCreateForm({ onCreated }: GuildCreateFormProps) {
  const { createGuild } = useGuilds();
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [iconUrl, setIconUrl] = useState('');
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError('');

    const trimmedName = name.trim();
    if (!trimmedName) {
      setError('Guild name is required.');
      return;
    }

    try {
      setIsSubmitting(true);
      const guild = await createGuild({
        name: trimmedName,
        description: description.trim() || undefined,
        icon_url: iconUrl.trim() || undefined
      });
      setName('');
      setDescription('');
      setIconUrl('');
      onCreated?.(guild);
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : 'Failed to create guild.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} className="grid gap-3">
      <label className="grid gap-2">
        <span className="text-sm font-semibold text-white/70">Guild name</span>
        <input
          value={name}
          onChange={(event) => setName(event.target.value)}
          placeholder="my guild"
          maxLength={100}
          className={inputClasses}
        />
      </label>
      <label className="grid gap-2">
        <span className="text-sm font-semibold text-white/70">Description (optional)</span>
        <input
          value={description}
          onChange={(event) => setDescription(event.target.value)}
          placeholder="what is this guild about?"
          className={inputClasses}
        />
      </label>
      <label className="grid gap-2">
        <span className="text-sm font-semibold text-white/70">Icon URL (optional)</span>
        <input
          value={iconUrl}
          onChange={(event) => setIconUrl(event.target.value)}
          placeholder="https://..."
          className={inputClasses}
        />
      </label>
      {error ? <FormError message={error} /> : null}
      <button type="submit" disabled={isSubmitting} className={submitClasses}>
        <Plus className="h-4 w-4" strokeWidth={2} />
        {isSubmitting ? 'Creating...' : 'Create guild'}
      </button>
    </form>
  );
}

type GuildJoinFormProps = {
  onJoined?: (guild: GuildDto) => void;
};

export function GuildJoinForm({ onJoined }: GuildJoinFormProps) {
  const { joinGuildWithCode } = useGuilds();
  const [code, setCode] = useState('');
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError('');

    const trimmedCode = code.trim();
    if (!trimmedCode) {
      setError('Invite code is required.');
      return;
    }

    try {
      setIsSubmitting(true);
      const guild = await joinGuildWithCode(trimmedCode);
      setCode('');
      onJoined?.(guild);
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : 'Failed to join guild.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} className="grid gap-3">
      <label className="grid gap-2">
        <span className="text-sm font-semibold text-white/70">Invite code</span>
        <input
          value={code}
          onChange={(event) => setCode(event.target.value)}
          placeholder="invite_code"
          className={inputClasses}
        />
      </label>
      {error ? <FormError message={error} /> : null}
      <button type="submit" disabled={isSubmitting} className={submitClasses}>
        {isSubmitting ? 'Joining...' : 'Join guild'}
      </button>
    </form>
  );
}
