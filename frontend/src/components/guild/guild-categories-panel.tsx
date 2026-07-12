'use client';

import { useCallback, useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Check, FolderPlus, Pencil, Trash2, X } from 'lucide-react';
import {
  createGuildCategory,
  deleteGuildCategory,
  listGuildCategories,
  updateGuildCategory,
  type GuildCategoryDto
} from '../../shared/api/guild';
import { ActionModal } from '../action-modal';
import { FormError } from './guild-forms';

const inputClasses =
  'h-10 w-full rounded-md border border-transparent bg-input-bg px-3 text-sm text-white outline-none transition placeholder:text-input-placeholder focus:border-aqua/35';

const iconButtonClasses =
  'flex h-8 w-8 shrink-0 items-center justify-center rounded-md text-[#8b8b8f] transition hover:bg-frame hover:text-white';

type GuildCategoriesPanelProps = {
  guildId: string;
};

export function GuildCategoriesPanel({ guildId }: GuildCategoriesPanelProps) {
  const [categories, setCategories] = useState<GuildCategoryDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [newName, setNewName] = useState('');
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editName, setEditName] = useState('');
  const [editPosition, setEditPosition] = useState('');
  const [isBusy, setIsBusy] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<GuildCategoryDto | null>(null);

  const load = useCallback(async () => {
    setIsLoading(true);
    setError('');

    try {
      const rows = await listGuildCategories(guildId);
      setCategories([...rows].sort((a, b) => a.position - b.position));
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : 'Failed to load categories.');
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

    const name = newName.trim();
    if (!name) {
      setError('Category name is required.');
      return;
    }

    try {
      setIsBusy(true);
      await createGuildCategory(guildId, { name });
      setNewName('');
      await load();
    } catch (createError) {
      setError(createError instanceof Error ? createError.message : 'Failed to create category.');
    } finally {
      setIsBusy(false);
    }
  }

  async function handleSaveEdit(categoryId: string) {
    setError('');

    const name = editName.trim();
    if (!name) {
      setError('Category name is required.');
      return;
    }

    const position = editPosition.trim() === '' ? undefined : Number(editPosition);
    if (position !== undefined && (!Number.isInteger(position) || position < 0)) {
      setError('Position must be a non-negative whole number.');
      return;
    }

    try {
      setIsBusy(true);
      await updateGuildCategory(guildId, categoryId, { name, position });
      setEditingId(null);
      await load();
    } catch (updateError) {
      setError(updateError instanceof Error ? updateError.message : 'Failed to update category.');
    } finally {
      setIsBusy(false);
    }
  }

  async function handleDelete(category: GuildCategoryDto) {
    setDeleteTarget(category);
  }

  async function confirmDeleteCategory() {
    if (!deleteTarget) {
      return;
    }

    setError('');

    try {
      setIsBusy(true);
      await deleteGuildCategory(guildId, deleteTarget.id);
      await load();
      setDeleteTarget(null);
    } catch (deleteError) {
      setError(deleteError instanceof Error ? deleteError.message : 'Failed to delete category.');
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
          <FolderPlus className="h-4 w-4 text-aqua" strokeWidth={1.9} />
          Create a category
        </h3>
        <div className="flex gap-3">
          <input
            value={newName}
            onChange={(event) => setNewName(event.target.value)}
            placeholder="category name"
            maxLength={100}
            className={inputClasses}
          />
          <button
            type="submit"
            disabled={isBusy}
            className="h-10 shrink-0 rounded-md bg-aqua px-5 text-sm font-bold text-primary-bg transition hover:bg-white disabled:cursor-not-allowed disabled:bg-frame disabled:text-white/25"
          >
            {isBusy ? 'Working...' : 'Create'}
          </button>
        </div>
      </form>

      {error ? <FormError message={error} /> : null}

      {isLoading ? (
        <div className="h-24 animate-pulse rounded-md bg-panel" />
      ) : categories.length === 0 ? (
        <p className="text-sm text-white/35">No categories yet.</p>
      ) : (
        <ul className="grid gap-2">
          {categories.map((category) => (
            <li
              key={category.id}
              className="flex items-center gap-3 rounded-md border border-stroke bg-panel px-3 py-2.5"
            >
              {editingId === category.id ? (
                <>
                  <input
                    value={editName}
                    onChange={(event) => setEditName(event.target.value)}
                    maxLength={100}
                    placeholder="category name"
                    className={inputClasses}
                  />
                  <input
                    value={editPosition}
                    onChange={(event) => setEditPosition(event.target.value)}
                    placeholder="position"
                    inputMode="numeric"
                    className={`${inputClasses} max-w-[6rem]`}
                  />
                  <button
                    type="button"
                    onClick={() => void handleSaveEdit(category.id)}
                    disabled={isBusy}
                    className={iconButtonClasses}
                    aria-label="Save category"
                  >
                    <Check className="h-4 w-4 text-lime" strokeWidth={2} />
                  </button>
                  <button
                    type="button"
                    onClick={() => setEditingId(null)}
                    className={iconButtonClasses}
                    aria-label="Cancel category edit"
                  >
                    <X className="h-4 w-4" strokeWidth={2} />
                  </button>
                </>
              ) : (
                <>
                  <div className="min-w-0 flex-1">
                    <p className="truncate text-[0.95rem] font-bold text-white">{category.name}</p>
                    <p className="text-xs text-white/35">position {category.position}</p>
                  </div>
                  <button
                    type="button"
                    onClick={() => {
                      setEditingId(category.id);
                      setEditName(category.name);
                      setEditPosition(String(category.position));
                    }}
                    className={iconButtonClasses}
                    aria-label={`Edit category ${category.name}`}
                  >
                    <Pencil className="h-4 w-4" strokeWidth={1.9} />
                  </button>
                  <button
                    type="button"
                    onClick={() => void handleDelete(category)}
                    disabled={isBusy}
                    className={`${iconButtonClasses} hover:text-pink`}
                    aria-label={`Delete category ${category.name}`}
                  >
                    <Trash2 className="h-4 w-4" strokeWidth={1.9} />
                  </button>
                </>
              )}
            </li>
          ))}
        </ul>
      )}

      {deleteTarget ? (
        <ActionModal
          title={`Delete category "${deleteTarget.name}"?`}
          description="The category will be removed and its channels will become uncategorised."
          confirmLabel="Delete category"
          destructive
          isBusy={isBusy}
          onClose={() => setDeleteTarget(null)}
          onConfirm={confirmDeleteCategory}
        />
      ) : null}
    </div>
  );
}
