'use client';

import { useCallback, useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Check, Hash, Pencil, Trash2, X } from 'lucide-react';
import {
  createGuildChannel,
  deleteGuildChannel,
  listGuildCategories,
  listGuildChannels,
  updateGuildChannel,
  type GuildCategoryDto,
  type GuildChannelDto
} from '../../shared/api/guild';
import { ActionModal } from '../action-modal';
import { useToast } from '../../shared/ui/toast';

const inputClasses =
  'h-10 w-full rounded-md border border-transparent bg-input-bg px-3 text-sm text-white outline-none transition placeholder:text-input-placeholder focus:border-aqua/35';

const selectClasses =
  'h-10 w-full rounded-md border border-transparent bg-input-bg px-3 text-sm text-white outline-none transition focus:border-aqua/35';

const iconButtonClasses =
  'flex h-8 w-8 shrink-0 items-center justify-center rounded-md text-[#8b8b8f] transition hover:bg-frame hover:text-white';

const UNCATEGORISED = '';

type EditDraft = {
  name: string;
  categoryId: string;
  topic: string;
  isNsfw: boolean;
  slowmodeSeconds: string;
  position: string;
};

type GuildChannelsPanelProps = {
  guildId: string;
  onChannelsChanged?: () => void;
};

function categoryName(categoryId: string | null | undefined, categories: GuildCategoryDto[]) {
  if (!categoryId) {
    return 'Uncategorised';
  }
  return categories.find((category) => category.id === categoryId)?.name ?? 'Uncategorised';
}

export function GuildChannelsPanel({ guildId, onChannelsChanged }: GuildChannelsPanelProps) {
  const [channels, setChannels] = useState<GuildChannelDto[]>([]);
  const [categories, setCategories] = useState<GuildCategoryDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [newName, setNewName] = useState('');
  const [newType, setNewType] = useState<'text' | 'announcement'>('text');
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editDraft, setEditDraft] = useState<EditDraft | null>(null);
  const [isBusy, setIsBusy] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<GuildChannelDto | null>(null);
  const { pushToast } = useToast();

  const load = useCallback(async () => {
    setIsLoading(true);
    setError('');

    try {
      const [channelRows, categoryRows] = await Promise.all([
        listGuildChannels(guildId),
        listGuildCategories(guildId)
      ]);
      setChannels([...channelRows].sort((a, b) => a.position - b.position));
      setCategories(categoryRows);
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : 'Failed to load channels.');
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
        title: 'Channels',
        description: error,
        tone: 'error'
      });
    }
  }, [error, pushToast]);

  async function handleCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError('');

    const name = newName.trim();
    if (!name) {
      setError('Channel name is required.');
      return;
    }

    try {
      setIsBusy(true);
      await createGuildChannel(guildId, { name, type: newType });
      setNewName('');
      setNewType('text');
      await load();
      onChannelsChanged?.();
    } catch (createError) {
      setError(createError instanceof Error ? createError.message : 'Failed to create channel.');
    } finally {
      setIsBusy(false);
    }
  }

  function startEdit(channel: GuildChannelDto) {
    setEditingId(channel.id);
    setEditDraft({
      name: channel.name,
      categoryId: channel.category_id ?? UNCATEGORISED,
      topic: channel.topic ?? '',
      isNsfw: channel.is_nsfw ?? false,
      slowmodeSeconds: String(channel.slowmode_seconds ?? 0),
      position: String(channel.position)
    });
  }

  function cancelEdit() {
    setEditingId(null);
    setEditDraft(null);
  }

  async function handleSaveEdit(channelId: string) {
    if (!editDraft) {
      return;
    }

    setError('');

    const name = editDraft.name.trim();
    if (!name) {
      setError('Channel name is required.');
      return;
    }

    const slowmodeSeconds = Number(editDraft.slowmodeSeconds);
    if (!Number.isInteger(slowmodeSeconds) || slowmodeSeconds < 0) {
      setError('Slowmode must be a non-negative whole number of seconds.');
      return;
    }

    const position = Number(editDraft.position);
    if (!Number.isInteger(position) || position < 0) {
      setError('Position must be a non-negative whole number.');
      return;
    }

    try {
      setIsBusy(true);
      await updateGuildChannel(guildId, channelId, {
        name,
        category_id: editDraft.categoryId || null,
        topic: editDraft.topic.trim() || null,
        is_nsfw: editDraft.isNsfw,
        slowmode_seconds: slowmodeSeconds,
        position
      });
      cancelEdit();
      await load();
      onChannelsChanged?.();
    } catch (updateError) {
      setError(updateError instanceof Error ? updateError.message : 'Failed to update channel.');
    } finally {
      setIsBusy(false);
    }
  }

  async function handleDelete(channel: GuildChannelDto) {
    setDeleteTarget(channel);
  }

  async function confirmDeleteChannel() {
    if (!deleteTarget) {
      return;
    }

    setError('');

    try {
      setIsBusy(true);
      await deleteGuildChannel(guildId, deleteTarget.id);
      await load();
      onChannelsChanged?.();
      setDeleteTarget(null);
    } catch (deleteError) {
      setError(deleteError instanceof Error ? deleteError.message : 'Failed to delete channel.');
    } finally {
      setIsBusy(false);
    }
  }

  return (
    <div className="grid gap-5">
      <form
        onSubmit={handleCreate}
        className="grid gap-3 rounded-md border border-stroke bg-panel p-4"
      >
        <h3 className="flex items-center gap-2 text-base font-bold text-white">
          <Hash className="h-4 w-4 text-aqua" strokeWidth={1.9} />
          Create a channel
        </h3>
        <div className="flex gap-3">
          <input
            value={newName}
            onChange={(event) => setNewName(event.target.value)}
            placeholder="channel name"
            maxLength={100}
            className={inputClasses}
          />
          <select
            value={newType}
            onChange={(event) => setNewType(event.target.value as 'text' | 'announcement')}
            className={`${selectClasses} max-w-[10rem] shrink-0`}
          >
            <option value="text">Text</option>
            <option value="announcement">Announcement</option>
          </select>
          <button
            type="submit"
            disabled={isBusy}
            className="h-10 shrink-0 rounded-md bg-aqua px-5 text-sm font-bold text-primary-bg transition hover:bg-white disabled:cursor-not-allowed disabled:bg-frame disabled:text-white/25"
          >
            {isBusy ? 'Working...' : 'Create'}
          </button>
        </div>
      </form>

      {isLoading ? (
        <div className="h-24 animate-pulse rounded-md bg-panel" />
      ) : channels.length === 0 ? (
        <p className="text-sm text-white/35">No channels yet.</p>
      ) : (
        <ul className="grid gap-2">
          {channels.map((channel) => (
            <li
              key={channel.id}
              className="grid gap-3 rounded-md border border-stroke bg-panel px-3 py-2.5"
            >
              {editingId === channel.id && editDraft ? (
                <div className="grid gap-3">
                  <div className="flex flex-wrap gap-3">
                    <input
                      value={editDraft.name}
                      onChange={(event) =>
                        setEditDraft({ ...editDraft, name: event.target.value })
                      }
                      maxLength={100}
                      placeholder="channel name"
                      className={`${inputClasses} max-w-[16rem]`}
                    />
                    <select
                      value={editDraft.categoryId}
                      onChange={(event) =>
                        setEditDraft({ ...editDraft, categoryId: event.target.value })
                      }
                      className={`${selectClasses} max-w-[12rem]`}
                    >
                      <option value={UNCATEGORISED}>Uncategorised</option>
                      {categories.map((category) => (
                        <option key={category.id} value={category.id}>
                          {category.name}
                        </option>
                      ))}
                    </select>
                    <input
                      value={editDraft.position}
                      onChange={(event) =>
                        setEditDraft({ ...editDraft, position: event.target.value })
                      }
                      placeholder="position"
                      inputMode="numeric"
                      className={`${inputClasses} max-w-[6rem]`}
                    />
                  </div>
                  <div className="flex flex-wrap items-center gap-3">
                    <input
                      value={editDraft.topic}
                      onChange={(event) =>
                        setEditDraft({ ...editDraft, topic: event.target.value })
                      }
                      placeholder="topic"
                      maxLength={200}
                      className={`${inputClasses} max-w-[20rem]`}
                    />
                    <input
                      value={editDraft.slowmodeSeconds}
                      onChange={(event) =>
                        setEditDraft({ ...editDraft, slowmodeSeconds: event.target.value })
                      }
                      placeholder="slowmode (s)"
                      inputMode="numeric"
                      className={`${inputClasses} max-w-[8rem]`}
                    />
                    <label className="flex items-center gap-2 text-sm text-white/60">
                      <input
                        type="checkbox"
                        checked={editDraft.isNsfw}
                        onChange={(event) =>
                          setEditDraft({ ...editDraft, isNsfw: event.target.checked })
                        }
                        className="h-4 w-4 accent-[#78dce8]"
                      />
                      NSFW
                    </label>
                    <div className="ml-auto flex gap-1">
                      <button
                        type="button"
                        onClick={() => void handleSaveEdit(channel.id)}
                        disabled={isBusy}
                        className={iconButtonClasses}
                        aria-label="Save channel"
                      >
                        <Check className="h-4 w-4 text-lime" strokeWidth={2} />
                      </button>
                      <button
                        type="button"
                        onClick={cancelEdit}
                        className={iconButtonClasses}
                        aria-label="Cancel channel edit"
                      >
                        <X className="h-4 w-4" strokeWidth={2} />
                      </button>
                    </div>
                  </div>
                </div>
              ) : (
                <div className="flex items-center gap-3">
                  <div className="min-w-0 flex-1">
                    <p className="flex items-center gap-1.5 truncate text-[0.95rem] font-bold text-white">
                      <Hash className="h-3.5 w-3.5 shrink-0 text-white/35" strokeWidth={1.9} />
                      {channel.name}
                      {channel.type === 'announcement' ? (
                        <span className="font-category rounded-full bg-aqua/15 px-2 py-0.5 text-[0.62rem] uppercase tracking-[0.1em] text-aqua">
                          Announcement
                        </span>
                      ) : null}
                    </p>
                    <p className="text-xs text-white/35">
                      {categoryName(channel.category_id, categories)} · position{' '}
                      {channel.position}
                    </p>
                  </div>
                  <button
                    type="button"
                    onClick={() => startEdit(channel)}
                    className={iconButtonClasses}
                    aria-label={`Edit channel ${channel.name}`}
                  >
                    <Pencil className="h-4 w-4" strokeWidth={1.9} />
                  </button>
                  <button
                    type="button"
                    onClick={() => void handleDelete(channel)}
                    disabled={isBusy}
                    className={`${iconButtonClasses} hover:text-pink`}
                    aria-label={`Delete channel ${channel.name}`}
                  >
                    <Trash2 className="h-4 w-4" strokeWidth={1.9} />
                  </button>
                </div>
              )}
            </li>
          ))}
        </ul>
      )}

      {deleteTarget ? (
        <ActionModal
          title={`Delete channel "${deleteTarget.name}"?`}
          description="This cannot be undone. Any messages in the channel will remain linked to the deleted channel."
          confirmLabel="Delete channel"
          destructive
          isBusy={isBusy}
          onClose={() => setDeleteTarget(null)}
          onConfirm={confirmDeleteChannel}
        />
      ) : null}
    </div>
  );
}
