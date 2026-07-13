'use client';

import { useCallback, useEffect, useMemo, useRef, useState, type RefObject } from 'react';
import { Check, Lock, Minus, Plus, Trash2, UserRound, X } from 'lucide-react';
import {
  deleteChannelPermissionOverwrite,
  listChannelPermissionOverwrites,
  putChannelPermissionOverwrite,
  type ChannelOverwriteDto,
  type GuildRoleDto
} from '../../shared/api/guild';
import { PERMISSIONS } from '../../shared/guilds/role-permissions';
import { useGuilds } from '../../shared/guilds/guild-store';
import { useGuildMembers, type HydratedGuildMember } from '../../shared/guilds/use-guild-members';
import { useCloseOnEscape } from '../../shared/hooks/use-close-on-escape';
import { useToast } from '../../shared/ui/toast';
import { ActionModal } from '../action-modal';

// Channel-scoped subset of the permission bitmask: bits that make sense as a
// per-channel allow/deny overwrite. Guild-wide bits (kick, ban, manage guild,
// administrator...) are deliberately left out of the editor.
export const OVERWRITE_FLAGS = [
  {
    value: PERMISSIONS.ReadMessages,
    label: 'Read messages',
    description: 'See this channel and its message history.'
  },
  {
    value: PERMISSIONS.SendMessages,
    label: 'Send messages',
    description: 'Post messages in this channel.'
  },
  {
    value: PERMISSIONS.ManageMessages,
    label: 'Manage messages',
    description: "Delete other members' messages in this channel."
  },
  {
    value: PERMISSIONS.MentionEveryone,
    label: 'Mention everyone',
    description: 'Use @everyone mentions in this channel.'
  },
  {
    value: PERMISSIONS.CreateInvite,
    label: 'Create invite',
    description: 'Create invites pointing to this channel.'
  },
  {
    value: PERMISSIONS.ManageChannels,
    label: 'Manage channel',
    description: 'Edit or delete this channel and its permissions.'
  }
];

export type OverwriteState = 'allow' | 'inherit' | 'deny';

export function overwriteKey(overwrite: Pick<ChannelOverwriteDto, 'target_type' | 'target_id'>) {
  return `${overwrite.target_type}:${overwrite.target_id}`;
}

export function bitState(overwrite: ChannelOverwriteDto, bit: number): OverwriteState {
  if ((overwrite.allow & bit) !== 0) {
    return 'allow';
  }
  if ((overwrite.deny & bit) !== 0) {
    return 'deny';
  }
  return 'inherit';
}

export function TriStateControl({
  state,
  disabled,
  onSelect
}: {
  state: OverwriteState;
  disabled: boolean;
  onSelect: (state: OverwriteState) => void;
}) {
  const options: { value: OverwriteState; icon: typeof Check; label: string; active: string }[] = [
    { value: 'deny', icon: X, label: 'Deny', active: 'bg-pink/20 text-pink' },
    { value: 'inherit', icon: Minus, label: 'Inherit', active: 'bg-frame text-white' },
    { value: 'allow', icon: Check, label: 'Allow', active: 'bg-lime/20 text-lime' }
  ];

  return (
    <div className="flex shrink-0 overflow-hidden rounded-md border border-stroke">
      {options.map((option) => {
        const Icon = option.icon;
        const isActive = state === option.value;

        return (
          <button
            key={option.value}
            type="button"
            disabled={disabled}
            onClick={() => onSelect(option.value)}
            title={option.label}
            aria-label={option.label}
            aria-pressed={isActive}
            className={`flex h-7 w-9 items-center justify-center transition disabled:cursor-not-allowed disabled:opacity-50 ${
              isActive ? option.active : 'text-[#8b8b8f] hover:bg-frame hover:text-white'
            }`}
          >
            <Icon className="h-3.5 w-3.5" strokeWidth={2.2} />
          </button>
        );
      })}
    </div>
  );
}

export function RoleDot({ role }: { role: GuildRoleDto }) {
  return (
    <span
      className="h-2.5 w-2.5 shrink-0 rounded-full"
      style={{ backgroundColor: role.color || '#8b8b8f' }}
    />
  );
}

type AddTargetPopoverProps = {
  roles: GuildRoleDto[];
  members: HydratedGuildMember[];
  onAdd: (targetType: 'role' | 'user', targetId: string) => void;
  onClose: () => void;
  /** Wrapper holding the trigger button and this popover; clicks outside it close the popover. */
  containerRef: RefObject<HTMLElement | null>;
};

export function AddTargetPopover({
  roles,
  members,
  onAdd,
  onClose,
  containerRef
}: AddTargetPopoverProps) {
  useEffect(() => {
    function handleMouseDown(event: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        onClose();
      }
    }

    document.addEventListener('mousedown', handleMouseDown);
    return () => document.removeEventListener('mousedown', handleMouseDown);
  }, [containerRef, onClose]);

  const rowClasses =
    'flex h-8 w-full items-center gap-2 rounded-md px-1.5 text-left text-sm text-white/60 transition hover:bg-frame hover:text-white';

  return (
    <div className="absolute left-0 top-9 z-20 w-64 rounded-md border border-stroke bg-panel p-2 shadow-lg">
      <div className="mb-1 flex items-center justify-between">
        <p className="px-1 text-xs font-bold uppercase tracking-wide text-white/40">
          Add role or member
        </p>
        <button
          type="button"
          onClick={onClose}
          className="flex h-6 w-6 items-center justify-center rounded-md text-[#8b8b8f] transition hover:bg-frame hover:text-white"
          aria-label="Close target list"
        >
          <X className="h-3.5 w-3.5" strokeWidth={2} />
        </button>
      </div>
      {roles.length === 0 && members.length === 0 ? (
        <p className="px-1 pb-1 text-sm text-white/35">
          Every role and member already has an overwrite.
        </p>
      ) : (
        <div className="grid max-h-64 gap-1 overflow-y-auto">
          {roles.length > 0 ? (
            <div>
              <p className="px-1 py-0.5 text-[0.65rem] font-bold uppercase tracking-wide text-white/30">
                Roles
              </p>
              {roles.map((role) => (
                <button
                  key={role.id}
                  type="button"
                  onClick={() => onAdd('role', role.id)}
                  className={rowClasses}
                >
                  <RoleDot role={role} />
                  <span className="min-w-0 flex-1 truncate">{role.name}</span>
                </button>
              ))}
            </div>
          ) : null}
          {members.length > 0 ? (
            <div>
              <p className="px-1 py-0.5 text-[0.65rem] font-bold uppercase tracking-wide text-white/30">
                Members
              </p>
              {members.map((member) => (
                <button
                  key={member.userId}
                  type="button"
                  onClick={() => onAdd('user', member.userId)}
                  className={rowClasses}
                >
                  <UserRound className="h-3.5 w-3.5 shrink-0 text-[#8a8a96]" strokeWidth={1.9} />
                  <span className="min-w-0 flex-1 truncate">{member.displayName}</span>
                </button>
              ))}
            </div>
          ) : null}
        </div>
      )}
    </div>
  );
}

type ChannelPermissionsModalProps = {
  guildId: string;
  channelId: string;
  channelName: string;
  onClose: () => void;
};

export function ChannelPermissionsModal({
  guildId,
  channelId,
  channelName,
  onClose
}: ChannelPermissionsModalProps) {
  const { selectedGuild } = useGuilds();
  const { members, roles } = useGuildMembers(
    guildId,
    selectedGuild?.id === guildId ? selectedGuild.owner_id : null
  );
  const [overwrites, setOverwrites] = useState<ChannelOverwriteDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [selectedKey, setSelectedKey] = useState<string | null>(null);
  const [busyKey, setBusyKey] = useState<string | null>(null);
  const [isAddOpen, setIsAddOpen] = useState(false);
  const [removeTarget, setRemoveTarget] = useState<ChannelOverwriteDto | null>(null);
  const [isRemoveBusy, setIsRemoveBusy] = useState(false);
  const [error, setError] = useState('');
  const addContainerRef = useRef<HTMLDivElement>(null);
  const { pushToast } = useToast();

  useCloseOnEscape(onClose);

  const load = useCallback(async () => {
    setIsLoading(true);
    setError('');

    try {
      setOverwrites(await listChannelPermissionOverwrites(channelId));
    } catch (loadError) {
      setError(
        loadError instanceof Error ? loadError.message : 'Failed to load channel permissions.'
      );
    } finally {
      setIsLoading(false);
    }
  }, [channelId]);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    if (error) {
      pushToast({
        title: 'Channel permissions',
        description: error,
        tone: 'error'
      });
    }
  }, [error, pushToast]);

  const rolesById = useMemo(() => new Map(roles.map((role) => [role.id, role])), [roles]);
  const membersById = useMemo(
    () => new Map(members.map((member) => [member.userId, member])),
    [members]
  );

  // Roles first (hierarchy order, @everyone last), then members by name;
  // targets whose role/member has since vanished sink to the bottom.
  const sortedOverwrites = useMemo(() => {
    const rolePosition = (overwrite: ChannelOverwriteDto) => {
      const role = rolesById.get(overwrite.target_id);
      if (!role) {
        return -Infinity;
      }
      return role.is_default ? -1 : role.position;
    };

    return [...overwrites].sort((a, b) => {
      if (a.target_type !== b.target_type) {
        return a.target_type === 'role' ? -1 : 1;
      }
      if (a.target_type === 'role') {
        return rolePosition(b) - rolePosition(a);
      }
      const aName = membersById.get(a.target_id)?.displayName ?? '';
      const bName = membersById.get(b.target_id)?.displayName ?? '';
      return aName.localeCompare(bName);
    });
  }, [overwrites, rolesById, membersById]);

  const selected = sortedOverwrites.find((overwrite) => overwriteKey(overwrite) === selectedKey);

  // Keep something sensible selected as overwrites load, get added or removed.
  useEffect(() => {
    if (!selected && sortedOverwrites.length > 0) {
      setSelectedKey(overwriteKey(sortedOverwrites[0]));
    }
  }, [selected, sortedOverwrites]);

  const overwrittenKeys = useMemo(
    () => new Set(overwrites.map((overwrite) => overwriteKey(overwrite))),
    [overwrites]
  );
  const availableRoles = useMemo(
    () =>
      roles
        .filter((role) => !overwrittenKeys.has(`role:${role.id}`))
        .sort((a, b) => {
          if (a.is_default !== b.is_default) {
            return a.is_default ? 1 : -1;
          }
          return b.position - a.position;
        }),
    [roles, overwrittenKeys]
  );
  const availableMembers = useMemo(
    () => members.filter((member) => !overwrittenKeys.has(`user:${member.userId}`)),
    [members, overwrittenKeys]
  );

  function targetName(overwrite: ChannelOverwriteDto) {
    if (overwrite.target_type === 'role') {
      return rolesById.get(overwrite.target_id)?.name ?? 'Unknown role';
    }
    return membersById.get(overwrite.target_id)?.displayName ?? 'Unknown member';
  }

  function patchLocal(key: string, allow: number, deny: number) {
    setOverwrites((current) =>
      current.map((overwrite) =>
        overwriteKey(overwrite) === key ? { ...overwrite, allow, deny } : overwrite
      )
    );
  }

  async function handleSetBit(overwrite: ChannelOverwriteDto, bit: number, state: OverwriteState) {
    const allow = state === 'allow' ? overwrite.allow | bit : overwrite.allow & ~bit;
    const deny = state === 'deny' ? overwrite.deny | bit : overwrite.deny & ~bit;

    if (allow === overwrite.allow && deny === overwrite.deny) {
      return;
    }

    const key = overwriteKey(overwrite);
    setError('');
    setBusyKey(key);
    patchLocal(key, allow, deny);

    try {
      await putChannelPermissionOverwrite(channelId, overwrite.target_id, {
        target_type: overwrite.target_type,
        allow,
        deny
      });
    } catch (putError) {
      patchLocal(key, overwrite.allow, overwrite.deny);
      setError(putError instanceof Error ? putError.message : 'Failed to update permissions.');
    } finally {
      setBusyKey(null);
    }
  }

  async function handleAddTarget(targetType: 'role' | 'user', targetId: string) {
    const created: ChannelOverwriteDto = {
      target_id: targetId,
      target_type: targetType,
      allow: 0,
      deny: 0
    };
    const key = overwriteKey(created);

    setIsAddOpen(false);
    setError('');
    setBusyKey(key);
    setOverwrites((current) => [...current, created]);
    setSelectedKey(key);

    try {
      await putChannelPermissionOverwrite(channelId, targetId, {
        target_type: targetType,
        allow: 0,
        deny: 0
      });
    } catch (addError) {
      setOverwrites((current) => current.filter((overwrite) => overwriteKey(overwrite) !== key));
      setSelectedKey(null);
      setError(addError instanceof Error ? addError.message : 'Failed to add overwrite.');
    } finally {
      setBusyKey(null);
    }
  }

  async function confirmRemoveOverwrite() {
    if (!removeTarget) {
      return;
    }

    setError('');

    try {
      setIsRemoveBusy(true);
      await deleteChannelPermissionOverwrite(channelId, removeTarget.target_id);
      const key = overwriteKey(removeTarget);
      setOverwrites((current) => current.filter((overwrite) => overwriteKey(overwrite) !== key));
      if (selectedKey === key) {
        setSelectedKey(null);
      }
      setRemoveTarget(null);
    } catch (removeError) {
      setError(removeError instanceof Error ? removeError.message : 'Failed to remove overwrite.');
    } finally {
      setIsRemoveBusy(false);
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/55 px-4 py-6">
      <button
        type="button"
        className="absolute inset-0 cursor-default"
        onClick={onClose}
        aria-label="Close channel permissions"
      />
      <section className="relative w-full max-w-[46rem] overflow-hidden rounded-[1rem] bg-secondary-bg shadow-2xl shadow-black/50 ring-1 ring-stroke">
        <div className="flex h-[4.75rem] items-center justify-between border-b border-stroke px-5">
          <div className="flex min-w-0 items-center gap-3">
            <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-md bg-aqua/10 text-aqua">
              <Lock className="h-5 w-5" strokeWidth={1.9} />
            </span>
            <div className="min-w-0">
              <h2 className="truncate text-[1.15rem] font-bold tracking-[-0.03em] text-white">
                #{channelName}
              </h2>
              <p className="font-category text-[0.7rem] uppercase tracking-[0.14em] text-white/35">
                Channel permissions
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="flex h-9 w-9 items-center justify-center rounded-md text-[#8b8b8f] transition hover:bg-frame hover:text-white"
            aria-label="Close channel permissions"
          >
            <X className="h-4 w-4" strokeWidth={2} />
          </button>
        </div>

        <div className="grid max-h-[calc(100vh-12rem)] min-h-[30rem] gap-4 overflow-y-auto p-5 sm:grid-cols-[14rem_minmax(0,1fr)]">
          <div className="min-w-0">
            <div className="relative flex items-center justify-between" ref={addContainerRef}>
              <p className="font-category px-1 text-[0.68rem] uppercase tracking-[0.14em] text-white/30">
                Roles &amp; members
              </p>
              <button
                type="button"
                onClick={() => setIsAddOpen((open) => !open)}
                className="flex h-7 w-7 items-center justify-center rounded-md text-[#8b8b8f] transition hover:bg-frame hover:text-white"
                aria-label="Add role or member overwrite"
                title="Add role or member"
              >
                <Plus className="h-4 w-4" strokeWidth={2} />
              </button>
              {isAddOpen ? (
                <AddTargetPopover
                  roles={availableRoles}
                  members={availableMembers}
                  onAdd={(targetType, targetId) => void handleAddTarget(targetType, targetId)}
                  onClose={() => setIsAddOpen(false)}
                  containerRef={addContainerRef}
                />
              ) : null}
            </div>

            {isLoading ? (
              <div className="mt-2 h-24 animate-pulse rounded-md bg-panel" />
            ) : sortedOverwrites.length === 0 ? (
              <div className="mt-2 grid gap-3">
                <p className="px-1 text-sm leading-6 text-white/35">
                  No overwrites yet. Add a role or member to customise who can see and use this
                  channel.
                </p>
                <button
                  type="button"
                  onClick={() => setIsAddOpen(true)}
                  className="flex h-10 items-center justify-center gap-2 rounded-md border border-dashed border-stroke-strong text-sm font-semibold text-white/50 transition hover:border-aqua/40 hover:text-aqua"
                >
                  <Plus className="h-4 w-4" strokeWidth={2} />
                  Add role or member
                </button>
              </div>
            ) : (
              <ul className="mt-2 grid gap-1">
                {sortedOverwrites.map((overwrite) => {
                  const key = overwriteKey(overwrite);
                  const isActive = key === selectedKey;
                  const role =
                    overwrite.target_type === 'role' ? rolesById.get(overwrite.target_id) : null;

                  return (
                    <li key={key}>
                      <button
                        type="button"
                        onClick={() => setSelectedKey(key)}
                        className={`flex h-9 w-full items-center gap-2 rounded-md px-2 text-left text-sm font-semibold transition ${
                          isActive ? 'bg-frame text-white' : 'text-white/60 hover:bg-frame/60'
                        }`}
                      >
                        {role ? (
                          <RoleDot role={role} />
                        ) : (
                          <UserRound
                            className="h-3.5 w-3.5 shrink-0 text-[#8a8a96]"
                            strokeWidth={1.9}
                          />
                        )}
                        <span className="min-w-0 flex-1 truncate">{targetName(overwrite)}</span>
                      </button>
                    </li>
                  );
                })}
              </ul>
            )}
          </div>

          <div className="min-w-0 rounded-md border border-stroke bg-panel p-4">
            {selected ? (
              <>
                <div className="flex items-center justify-between gap-3">
                  <div className="min-w-0">
                    <p className="truncate text-[0.95rem] font-bold text-white">
                      {targetName(selected)}
                    </p>
                    <p className="text-xs text-white/35">
                      {selected.target_type === 'role' ? 'Role overwrite' : 'Member overwrite'}
                    </p>
                  </div>
                  <button
                    type="button"
                    onClick={() => setRemoveTarget(selected)}
                    className="flex h-8 w-8 shrink-0 items-center justify-center rounded-md text-[#8b8b8f] transition hover:bg-frame hover:text-pink"
                    aria-label={`Remove overwrite for ${targetName(selected)}`}
                    title="Remove overwrite"
                  >
                    <Trash2 className="h-4 w-4" strokeWidth={1.9} />
                  </button>
                </div>

                <ul className="mt-4 grid gap-2">
                  {OVERWRITE_FLAGS.map((flag) => (
                    <li
                      key={flag.value}
                      className="flex items-center justify-between gap-4 rounded-md border border-stroke bg-secondary-bg px-3 py-2"
                    >
                      <div className="min-w-0">
                        <p className="text-sm font-semibold text-white/80">{flag.label}</p>
                        <p className="text-xs leading-5 text-white/35">{flag.description}</p>
                      </div>
                      <TriStateControl
                        state={bitState(selected, flag.value)}
                        disabled={busyKey === overwriteKey(selected)}
                        onSelect={(state) => void handleSetBit(selected, flag.value, state)}
                      />
                    </li>
                  ))}
                </ul>

                <p className="mt-3 text-xs leading-5 text-white/35">
                  Inherit falls back to the permissions granted by the member&apos;s roles. Member
                  overwrites win over role overwrites; deny wins over allow at the same level.
                </p>
              </>
            ) : (
              <div className="flex h-full items-center justify-center">
                <p className="max-w-[18rem] text-center text-sm leading-6 text-white/35">
                  {sortedOverwrites.length === 0
                    ? 'This channel has no overwrites: everyone sees it with the permissions their roles grant.'
                    : 'Select a role or member on the left to edit what it can do in this channel.'}
                </p>
              </div>
            )}
          </div>
        </div>
      </section>

      {removeTarget ? (
        <ActionModal
          title={`Remove overwrite for ${targetName(removeTarget)}?`}
          description="This channel will fall back to the permissions granted by roles alone."
          confirmLabel="Remove overwrite"
          destructive
          isBusy={isRemoveBusy}
          onClose={() => setRemoveTarget(null)}
          onConfirm={confirmRemoveOverwrite}
        />
      ) : null}
    </div>
  );
}
