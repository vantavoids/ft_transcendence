'use client';

import { useCallback, useEffect, useState } from 'react';
import { ShieldPlus, Trash2 } from 'lucide-react';
import {
  createGuildRole,
  deleteGuildRole,
  listGuildRoles,
  updateGuildRole,
  type GuildRoleDto
} from '../../shared/api/guild';
import { ActionModal } from '../action-modal';
import { FormError } from './guild-forms';

const inputClasses =
  'h-10 w-full rounded-md border border-transparent bg-input-bg px-3 text-sm text-white outline-none transition placeholder:text-input-placeholder focus:border-aqua/35';

const PERMISSION_FLAGS = [
  { label: 'Send messages', value: 1 },
  { label: 'Read messages', value: 2 },
  { label: 'Manage messages', value: 4 },
  { label: 'Manage channels', value: 8 },
  { label: 'Kick members', value: 16 },
  { label: 'Ban members', value: 32 },
  { label: 'Manage roles', value: 64 },
  { label: 'Manage guild', value: 128 },
  { label: 'Administrator', value: 256 },
  { label: 'Create invite', value: 512 },
  { label: 'Mention everyone', value: 1024 },
  { label: 'Manage nicknames', value: 2048 }
];

type RoleDraft = {
  name: string;
  color: string;
  permissions: number;
  isHoisted: boolean;
  isMentionable: boolean;
};

const emptyDraft: RoleDraft = {
  name: '',
  color: '#78dce8',
  permissions: 0,
  isHoisted: false,
  isMentionable: false
};

function draftFromRole(role: GuildRoleDto): RoleDraft {
  return {
    name: role.name,
    color: role.color,
    permissions: Number(role.permissions) || 0,
    isHoisted: role.is_hoisted,
    isMentionable: role.is_mentionable
  };
}

type RoleEditorProps = {
  draft: RoleDraft;
  onChange: (draft: RoleDraft) => void;
  onSubmit: () => void;
  onCancel?: () => void;
  submitLabel: string;
  isBusy: boolean;
  isNameLocked?: boolean;
};

function RoleEditor({
  draft,
  onChange,
  onSubmit,
  onCancel,
  submitLabel,
  isBusy,
  isNameLocked = false
}: RoleEditorProps) {
  return (
    <div className="grid gap-3">
      <div className="grid gap-3 sm:grid-cols-[1fr_auto]">
        <input
          value={draft.name}
          onChange={(event) => onChange({ ...draft, name: event.target.value })}
          placeholder="role name"
          maxLength={100}
          disabled={isNameLocked}
          title={isNameLocked ? 'The default role name cannot be changed.' : undefined}
          className={`${inputClasses} disabled:cursor-not-allowed disabled:opacity-50`}
        />
        <label className="flex items-center gap-2 text-sm text-white/55">
          Color
          <input
            type="color"
            value={draft.color}
            onChange={(event) => onChange({ ...draft, color: event.target.value })}
            className="h-10 w-14 cursor-pointer rounded-md border border-stroke bg-input-bg p-1"
          />
        </label>
      </div>
      <div className="grid grid-cols-2 gap-x-4 gap-y-1.5 sm:grid-cols-3">
        {PERMISSION_FLAGS.map((flag) => (
          <label key={flag.value} className="flex items-center gap-2 text-sm text-white/60">
            <input
              type="checkbox"
              checked={(draft.permissions & flag.value) !== 0}
              onChange={(event) =>
                onChange({
                  ...draft,
                  permissions: event.target.checked
                    ? draft.permissions | flag.value
                    : draft.permissions & ~flag.value
                })
              }
              className="h-4 w-4 accent-[#78dce8]"
            />
            {flag.label}
          </label>
        ))}
      </div>
      <div className="flex flex-wrap gap-4">
        <label className="flex items-center gap-2 text-sm text-white/60">
          <input
            type="checkbox"
            checked={draft.isHoisted}
            onChange={(event) => onChange({ ...draft, isHoisted: event.target.checked })}
            className="h-4 w-4 accent-[#78dce8]"
          />
          Display separately (hoisted)
        </label>
        <label className="flex items-center gap-2 text-sm text-white/60">
          <input
            type="checkbox"
            checked={draft.isMentionable}
            onChange={(event) => onChange({ ...draft, isMentionable: event.target.checked })}
            className="h-4 w-4 accent-[#78dce8]"
          />
          Mentionable
        </label>
      </div>
      <div className="flex gap-3">
        <button
          type="button"
          onClick={onSubmit}
          disabled={isBusy}
          className="h-10 rounded-md bg-aqua px-5 text-sm font-bold text-primary-bg transition hover:bg-white disabled:cursor-not-allowed disabled:bg-frame disabled:text-white/25"
        >
          {submitLabel}
        </button>
        {onCancel ? (
          <button
            type="button"
            onClick={onCancel}
            className="h-10 rounded-md border border-stroke px-5 text-sm font-bold text-white/60 transition hover:text-white"
          >
            Cancel
          </button>
        ) : null}
      </div>
    </div>
  );
}

type GuildRolesPanelProps = {
  guildId: string;
};

export function GuildRolesPanel({ guildId }: GuildRolesPanelProps) {
  const [roles, setRoles] = useState<GuildRoleDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [createDraft, setCreateDraft] = useState<RoleDraft>(emptyDraft);
  const [editingRoleId, setEditingRoleId] = useState<string | null>(null);
  const [editDraft, setEditDraft] = useState<RoleDraft>(emptyDraft);
  const [isBusy, setIsBusy] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<GuildRoleDto | null>(null);

  const load = useCallback(async () => {
    setIsLoading(true);
    setError('');

    try {
      const rows = await listGuildRoles(guildId);
      setRoles([...rows].sort((a, b) => b.position - a.position));
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : 'Failed to load roles.');
    } finally {
      setIsLoading(false);
    }
  }, [guildId]);

  useEffect(() => {
    void load();
  }, [load]);

  async function handleCreate() {
    setError('');

    if (!createDraft.name.trim()) {
      setError('Role name is required.');
      return;
    }

    try {
      setIsBusy(true);
      await createGuildRole(guildId, {
        name: createDraft.name.trim(),
        color: createDraft.color,
        permissions: createDraft.permissions,
        is_hoisted: createDraft.isHoisted,
        is_mentionable: createDraft.isMentionable
      });
      setCreateDraft(emptyDraft);
      await load();
    } catch (createError) {
      setError(createError instanceof Error ? createError.message : 'Failed to create role.');
    } finally {
      setIsBusy(false);
    }
  }

  async function handleUpdate(roleId: string) {
    setError('');

    const role = roles.find((r) => r.id === roleId);
    const isDefault = role?.is_default ?? false;

    if (!isDefault && !editDraft.name.trim()) {
      setError('Role name is required.');
      return;
    }

    try {
      setIsBusy(true);
      await updateGuildRole(guildId, roleId, {
        ...(isDefault ? {} : { name: editDraft.name.trim() }),
        color: editDraft.color,
        permissions: editDraft.permissions,
        is_hoisted: editDraft.isHoisted,
        is_mentionable: editDraft.isMentionable
      });
      setEditingRoleId(null);
      await load();
    } catch (updateError) {
      setError(updateError instanceof Error ? updateError.message : 'Failed to update role.');
    } finally {
      setIsBusy(false);
    }
  }

  async function handleDelete(role: GuildRoleDto) {
    setDeleteTarget(role);
  }

  async function confirmDeleteRole() {
    if (!deleteTarget) {
      return;
    }

    setError('');

    try {
      setIsBusy(true);
      await deleteGuildRole(guildId, deleteTarget.id);
      await load();
      setDeleteTarget(null);
    } catch (deleteError) {
      setError(deleteError instanceof Error ? deleteError.message : 'Failed to delete role.');
    } finally {
      setIsBusy(false);
    }
  }

  return (
    <div className="grid gap-5">
      <div className="grid gap-3 rounded-md border border-stroke bg-panel p-4">
        <h3 className="flex items-center gap-2 text-base font-bold text-white">
          <ShieldPlus className="h-4 w-4 text-aqua" strokeWidth={1.9} />
          Create a role
        </h3>
        <RoleEditor
          draft={createDraft}
          onChange={setCreateDraft}
          onSubmit={() => void handleCreate()}
          submitLabel={isBusy ? 'Creating...' : 'Create role'}
          isBusy={isBusy}
        />
      </div>

      {error ? <FormError message={error} /> : null}

      {isLoading ? (
        <div className="h-24 animate-pulse rounded-md bg-panel" />
      ) : (
        <ul className="grid gap-2">
          {roles.map((role) => (
            <li key={role.id} className="rounded-md border border-stroke bg-panel px-3 py-2.5">
              {editingRoleId === role.id ? (
                <RoleEditor
                  draft={editDraft}
                  onChange={setEditDraft}
                  onSubmit={() => void handleUpdate(role.id)}
                  onCancel={() => setEditingRoleId(null)}
                  submitLabel={isBusy ? 'Saving...' : 'Save role'}
                  isBusy={isBusy}
                  isNameLocked={role.is_default}
                />
              ) : (
                <div className="flex items-center gap-3">
                  <span
                    className="h-3.5 w-3.5 shrink-0 rounded-full"
                    style={{ backgroundColor: role.color || '#8b8b8f' }}
                  />
                  <div className="min-w-0 flex-1">
                    <p className="truncate text-[0.95rem] font-bold text-white">
                      {role.name}
                      {role.is_default ? (
                        <span className="ml-2 rounded bg-frame px-1.5 py-0.5 text-[0.65rem] font-bold uppercase tracking-wide text-white/40">
                          default
                        </span>
                      ) : null}
                    </p>
                    <p className="truncate text-xs text-white/35">
                      position {role.position} · permissions {Number(role.permissions) || 0}
                      {role.is_hoisted ? ' · hoisted' : ''}
                      {role.is_mentionable ? ' · mentionable' : ''}
                    </p>
                  </div>
                  <button
                    type="button"
                    onClick={() => {
                      setEditDraft(draftFromRole(role));
                      setEditingRoleId(role.id);
                    }}
                    className="h-8 shrink-0 rounded-md border border-stroke px-3 text-xs font-bold text-white/60 transition hover:border-aqua/40 hover:text-aqua"
                  >
                    Edit
                  </button>
                  {!role.is_default ? (
                    <button
                      type="button"
                      onClick={() => void handleDelete(role)}
                      disabled={isBusy}
                      className="flex h-8 w-8 shrink-0 items-center justify-center rounded-md text-[#8b8b8f] transition hover:bg-frame hover:text-pink"
                      aria-label={`Delete role ${role.name}`}
                    >
                      <Trash2 className="h-4 w-4" strokeWidth={1.9} />
                    </button>
                  ) : null}
                </div>
              )}
            </li>
          ))}
        </ul>
      )}

      {deleteTarget ? (
        <ActionModal
          title={`Delete role "${deleteTarget.name}"?`}
          description="This will remove the role from the guild and from every member who has it."
          confirmLabel="Delete role"
          destructive
          isBusy={isBusy}
          onClose={() => setDeleteTarget(null)}
          onConfirm={confirmDeleteRole}
        />
      ) : null}
    </div>
  );
}
