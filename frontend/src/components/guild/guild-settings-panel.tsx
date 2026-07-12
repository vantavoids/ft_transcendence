'use client';

import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { AlertTriangle, Save } from 'lucide-react';
import { deleteGuild, getGuild, updateGuild, type GuildDto } from '../../shared/api/guild';
import { useGuilds } from '../../shared/guilds/guild-store';
import { ActionModal } from '../action-modal';
import { useToast } from '../../shared/ui/toast';

const inputClasses =
  'h-11 w-full rounded-md border border-transparent bg-input-bg px-4 text-base text-white outline-none transition placeholder:text-input-placeholder focus:border-aqua/35';

type GuildSettingsPanelProps = {
  guildId: string;
};

export function GuildSettingsPanel({ guildId }: GuildSettingsPanelProps) {
  const { currentUserId, refreshGuilds } = useGuilds();
  const [guild, setGuild] = useState<GuildDto | null>(null);
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [iconUrl, setIconUrl] = useState('');
  const [bannerUrl, setBannerUrl] = useState('');
  const [error, setError] = useState('');
  const [savedMessage, setSavedMessage] = useState('');
  const [isSaving, setIsSaving] = useState(false);
  const [deleteConfirm, setDeleteConfirm] = useState('');
  const [deleteError, setDeleteError] = useState('');
  const [isDeleting, setIsDeleting] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const { pushToast } = useToast();

  useEffect(() => {
    let isCancelled = false;

    getGuild(guildId)
      .then((details) => {
        if (isCancelled) {
          return;
        }

        setGuild(details);
        setName(details.name);
        setDescription(details.description ?? '');
        setIconUrl(details.icon_url ?? '');
        setBannerUrl(details.banner_url ?? '');
      })
      .catch((loadError) => {
        if (!isCancelled) {
          setError(loadError instanceof Error ? loadError.message : 'Failed to load guild.');
        }
      });

    return () => {
      isCancelled = true;
    };
  }, [guildId]);

  useEffect(() => {
    if (error) {
      pushToast({
        title: 'Guild settings',
        description: error,
        tone: 'error'
      });
    }
  }, [error, pushToast]);

  useEffect(() => {
    if (deleteError) {
      pushToast({
        title: 'Guild deletion',
        description: deleteError,
        tone: 'error'
      });
    }
  }, [deleteError, pushToast]);

  // The server enforces ownership; without a resolved current user we still
  // show the section so the fake-session dev flow can exercise it.
  const canShowDelete = !currentUserId || guild?.owner_id === currentUserId;

  async function handleSave(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError('');
    setSavedMessage('');

    const trimmedName = name.trim();
    if (!trimmedName) {
      setError('Guild name is required.');
      return;
    }

    try {
      setIsSaving(true);
      const updated = await updateGuild(guildId, {
        name: trimmedName,
        description: description.trim() || undefined,
        icon_url: iconUrl.trim() || undefined,
        banner_url: bannerUrl.trim() || undefined
      });
      setGuild(updated);
      setSavedMessage('Guild settings saved.');
      void refreshGuilds();
    } catch (saveError) {
      setError(saveError instanceof Error ? saveError.message : 'Failed to update guild.');
    } finally {
      setIsSaving(false);
    }
  }

  async function handleDelete(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setDeleteError('');

    if (!guild || deleteConfirm.trim() !== guild.name) {
      setDeleteError('Type the exact guild name to confirm deletion.');
      return;
    }

    setIsDeleteModalOpen(true);
  }

  async function confirmDeleteGuild() {
    if (!guild) {
      return;
    }

    setDeleteError('');

    try {
      setIsDeleting(true);
      await deleteGuild(guildId);
      await refreshGuilds();
      setIsDeleteModalOpen(false);
    } catch (removeError) {
      setDeleteError(
        removeError instanceof Error ? removeError.message : 'Failed to delete guild.'
      );
    } finally {
      setIsDeleting(false);
    }
  }

  return (
    <div className="grid gap-5">
      <form
        onSubmit={handleSave}
        className="grid gap-3 rounded-md border border-stroke bg-panel p-4"
      >
        <h3 className="text-base font-bold text-white">Guild settings</h3>
        <label className="grid gap-2">
          <span className="text-sm font-semibold text-white/70">Name</span>
          <input
            value={name}
            onChange={(event) => setName(event.target.value)}
            maxLength={100}
            className={inputClasses}
          />
        </label>
        <label className="grid gap-2">
          <span className="text-sm font-semibold text-white/70">Description</span>
          <input
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            placeholder="describe your guild"
            className={inputClasses}
          />
        </label>
        <div className="grid gap-3 sm:grid-cols-2">
          <label className="grid gap-2">
            <span className="text-sm font-semibold text-white/70">Icon URL</span>
            <input
              value={iconUrl}
              onChange={(event) => setIconUrl(event.target.value)}
              placeholder="https://..."
              className={inputClasses}
            />
          </label>
          <label className="grid gap-2">
            <span className="text-sm font-semibold text-white/70">Banner URL</span>
            <input
              value={bannerUrl}
              onChange={(event) => setBannerUrl(event.target.value)}
              placeholder="https://..."
              className={inputClasses}
            />
          </label>
        </div>
        {savedMessage ? (
          <p className="rounded-md border border-lime/30 bg-lime/10 px-3 py-2 text-sm text-lime">
            {savedMessage}
          </p>
        ) : null}
        <button
          type="submit"
          disabled={isSaving || !guild}
          className="flex h-10 w-fit items-center gap-2 rounded-md bg-aqua px-5 text-sm font-bold text-primary-bg transition hover:bg-white disabled:cursor-not-allowed disabled:bg-frame disabled:text-white/25"
        >
          <Save className="h-4 w-4" strokeWidth={2} />
          {isSaving ? 'Saving...' : 'Save changes'}
        </button>
      </form>

      {canShowDelete ? (
        <form
          onSubmit={handleDelete}
          className="grid gap-3 rounded-md border border-pink/25 bg-pink/5 p-4"
        >
          <h3 className="flex items-center gap-2 text-base font-bold text-pink">
            <AlertTriangle className="h-4 w-4" strokeWidth={1.9} />
            Danger zone
          </h3>
          <p className="text-sm text-white/55">
            Deleting a guild is permanent and only the owner can do it. Type{' '}
            <span className="mono-detail text-white">{guild?.name ?? '...'}</span> to confirm.
          </p>
          <div className="flex flex-wrap gap-3">
            <input
              value={deleteConfirm}
              onChange={(event) => setDeleteConfirm(event.target.value)}
              placeholder="guild name"
              className={`${inputClasses} max-w-[18rem]`}
            />
            <button
              type="submit"
              disabled={isDeleting || !guild || deleteConfirm.trim() !== guild.name}
              className="h-11 rounded-md border border-pink/25 bg-pink/10 px-5 text-sm font-bold text-pink transition hover:border-pink/45 hover:bg-pink/15 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {isDeleting ? 'Deleting...' : 'Delete guild'}
            </button>
          </div>
        </form>
      ) : null}

      {isDeleteModalOpen && guild ? (
        <ActionModal
          title={`Delete "${guild.name}"?`}
          description={
            <>
              This will permanently delete the guild and remove all of its data. This cannot be
              undone.
            </>
          }
          confirmLabel="Delete guild"
          destructive
          isBusy={isDeleting}
          onClose={() => setIsDeleteModalOpen(false)}
          onConfirm={confirmDeleteGuild}
        />
      ) : null}
    </div>
  );
}
