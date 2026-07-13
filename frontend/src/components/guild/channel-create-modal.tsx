'use client';

import { useEffect, useMemo, useRef, useState } from 'react';
import { Hash, Plus, Trash2, UserRound, X } from 'lucide-react';
import {
  createGuildChannel,
  type ChannelOverwriteDto,
  type GuildChannelDto
} from '../../shared/api/guild';
import { useGuilds } from '../../shared/guilds/guild-store';
import { useGuildMembers } from '../../shared/guilds/use-guild-members';
import { useCloseOnEscape } from '../../shared/hooks/use-close-on-escape';
import { useToast } from '../../shared/ui/toast';
import {
  AddTargetPopover,
  bitState,
  OVERWRITE_FLAGS,
  overwriteKey,
  RoleDot,
  TriStateControl,
  type OverwriteState
} from './channel-permissions-modal';

const inputClasses =
  'h-10 w-full rounded-md border border-transparent bg-input-bg px-3 text-sm text-white outline-none transition placeholder:text-input-placeholder focus:border-aqua/35';

const selectClasses =
  'h-10 w-full rounded-md border border-transparent bg-input-bg px-3 text-sm text-white outline-none transition focus:border-aqua/35';

type ChannelCreateModalProps = {
  guildId: string;
  initialName?: string;
  initialType?: 'text' | 'announcement';
  onClose: () => void;
  onCreated: (channel: GuildChannelDto) => void;
};

export function ChannelCreateModal({
  guildId,
  initialName = '',
  initialType = 'text',
  onClose,
  onCreated
}: ChannelCreateModalProps) {
  const { selectedGuild } = useGuilds();
  const { members, roles } = useGuildMembers(
    guildId,
    selectedGuild?.id === guildId ? selectedGuild.owner_id : null
  );
  const [name, setName] = useState(initialName);
  const [type, setType] = useState<'text' | 'announcement'>(initialType);
  // overwrites are staged entirely in memory here and sent atomically with the
  // channel on submit, so it is created already carrying these permissions.
  const [overwrites, setOverwrites] = useState<ChannelOverwriteDto[]>([]);
  const [selectedKey, setSelectedKey] = useState<string | null>(null);
  const [isAddOpen, setIsAddOpen] = useState(false);
  const [isBusy, setIsBusy] = useState(false);
  const [error, setError] = useState('');
  const addContainerRef = useRef<HTMLDivElement>(null);
  const { pushToast } = useToast();

  useCloseOnEscape(onClose);

  useEffect(() => {
    if (error) {
      pushToast({ title: 'Create channel', description: error, tone: 'error' });
    }
  }, [error, pushToast]);

  const rolesById = useMemo(() => new Map(roles.map((role) => [role.id, role])), [roles]);
  const membersById = useMemo(
    () => new Map(members.map((member) => [member.userId, member])),
    [members]
  );

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

  // roles first (hierarchy order, @everyone last), then members by name.
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

  useEffect(() => {
    if (!selected && sortedOverwrites.length > 0) {
      setSelectedKey(overwriteKey(sortedOverwrites[0]));
    }
  }, [selected, sortedOverwrites]);

  function targetName(overwrite: ChannelOverwriteDto) {
    if (overwrite.target_type === 'role') {
      return rolesById.get(overwrite.target_id)?.name ?? 'Unknown role';
    }
    return membersById.get(overwrite.target_id)?.displayName ?? 'Unknown member';
  }

  function handleAddTarget(targetType: 'role' | 'user', targetId: string) {
    const created: ChannelOverwriteDto = {
      target_id: targetId,
      target_type: targetType,
      allow: 0,
      deny: 0
    };
    setIsAddOpen(false);
    setOverwrites((current) => [...current, created]);
    setSelectedKey(overwriteKey(created));
  }

  function handleRemove(overwrite: ChannelOverwriteDto) {
    const key = overwriteKey(overwrite);
    setOverwrites((current) => current.filter((item) => overwriteKey(item) !== key));
    if (selectedKey === key) {
      setSelectedKey(null);
    }
  }

  function handleSetBit(overwrite: ChannelOverwriteDto, bit: number, state: OverwriteState) {
    const allow = state === 'allow' ? overwrite.allow | bit : overwrite.allow & ~bit;
    const deny = state === 'deny' ? overwrite.deny | bit : overwrite.deny & ~bit;
    const key = overwriteKey(overwrite);
    setOverwrites((current) =>
      current.map((item) => (overwriteKey(item) === key ? { ...item, allow, deny } : item))
    );
  }

  async function handleSubmit() {
    const trimmed = name.trim();
    if (!trimmed) {
      setError('Channel name is required.');
      return;
    }

    setError('');

    try {
      setIsBusy(true);
      const channel = await createGuildChannel(guildId, {
        name: trimmed,
        type,
        // drop no-op overwrites (allow & deny both empty) so we never create a row
        // that has no effect; the backend would accept them but they add noise.
        overwrites: overwrites
          .filter((overwrite) => overwrite.allow !== 0 || overwrite.deny !== 0)
          .map((overwrite) => ({
            target_id: overwrite.target_id,
            target_type: overwrite.target_type,
            allow: overwrite.allow,
            deny: overwrite.deny
          }))
      });
      onCreated(channel);
    } catch (createError) {
      setError(createError instanceof Error ? createError.message : 'Failed to create channel.');
    } finally {
      setIsBusy(false);
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/55 px-4 py-6">
      <button
        type="button"
        className="absolute inset-0 cursor-default"
        onClick={onClose}
        aria-label="Close create channel"
      />
      <section className="relative w-full max-w-[46rem] overflow-hidden rounded-[1rem] bg-secondary-bg shadow-2xl shadow-black/50 ring-1 ring-stroke">
        <div className="flex h-[4.75rem] items-center justify-between border-b border-stroke px-5">
          <div className="flex min-w-0 items-center gap-3">
            <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-md bg-aqua/10 text-aqua">
              <Hash className="h-5 w-5" strokeWidth={1.9} />
            </span>
            <div className="min-w-0">
              <h2 className="truncate text-[1.15rem] font-bold tracking-[-0.03em] text-white">
                Create channel
              </h2>
              <p className="font-category text-[0.7rem] uppercase tracking-[0.14em] text-white/35">
                Name, type &amp; permissions
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="flex h-9 w-9 items-center justify-center rounded-md text-[#8b8b8f] transition hover:bg-frame hover:text-white"
            aria-label="Close create channel"
          >
            <X className="h-4 w-4" strokeWidth={2} />
          </button>
        </div>

        <div className="max-h-[calc(100vh-12rem)] overflow-y-auto p-5">
          <div className="flex flex-wrap gap-3">
            <input
              value={name}
              onChange={(event) => setName(event.target.value)}
              placeholder="channel name"
              maxLength={100}
              autoFocus
              className={`${inputClasses} max-w-[20rem]`}
            />
            <select
              value={type}
              onChange={(event) => setType(event.target.value as 'text' | 'announcement')}
              className={`${selectClasses} max-w-[12rem]`}
            >
              <option value="text">Text</option>
              <option value="announcement">Announcement</option>
            </select>
          </div>

          <div className="mt-5 grid gap-4 sm:grid-cols-[14rem_minmax(0,1fr)]">
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
                    onAdd={handleAddTarget}
                    onClose={() => setIsAddOpen(false)}
                    containerRef={addContainerRef}
                  />
                ) : null}
              </div>

              {sortedOverwrites.length === 0 ? (
                <div className="mt-2 grid gap-3">
                  <p className="px-1 text-sm leading-6 text-white/35">
                    No overwrites yet. Add a role or member to control who can see and use this
                    channel from the moment it is created.
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
                      onClick={() => handleRemove(selected)}
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
                          disabled={isBusy}
                          onSelect={(state) => handleSetBit(selected, flag.value, state)}
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
                      ? 'This channel will use the permissions each role grants. Add an overwrite to make it private or grant extra access.'
                      : 'Select a role or member on the left to edit what it can do in this channel.'}
                  </p>
                </div>
              )}
            </div>
          </div>
        </div>

        <div className="flex items-center justify-end gap-3 border-t border-stroke px-5 py-4">
          <button
            type="button"
            onClick={onClose}
            className="h-10 rounded-md border border-stroke px-5 text-sm font-bold text-white/60 transition hover:text-white"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={() => void handleSubmit()}
            disabled={isBusy}
            className="h-10 rounded-md bg-aqua px-5 text-sm font-bold text-primary-bg transition hover:bg-white disabled:cursor-not-allowed disabled:bg-frame disabled:text-white/25"
          >
            {isBusy ? 'Creating...' : 'Create channel'}
          </button>
        </div>
      </section>
    </div>
  );
}
