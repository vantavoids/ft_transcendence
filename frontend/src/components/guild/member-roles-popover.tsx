'use client';

import { useEffect, useState, type RefObject } from 'react';
import { X } from 'lucide-react';
import {
  assignGuildMemberRole,
  unassignGuildMemberRole,
  type GuildRoleDto
} from '../../shared/api/guild';
import { canToggleRole, type RoleCaller } from '../../shared/guilds/role-permissions';
import type { HydratedGuildMember } from '../../shared/guilds/use-guild-members';
import { useCloseOnEscape } from '../../shared/hooks/use-close-on-escape';
import { useToast } from '../../shared/ui/toast';

export function MemberRoleChips({ roles }: { roles: GuildRoleDto[] }) {
  if (roles.length === 0) {
    return null;
  }

  return (
    <span className="mt-1 flex flex-wrap gap-1">
      {roles.map((role) => (
        <span
          key={role.id}
          className="inline-flex items-center gap-1.5 rounded-full border border-stroke bg-frame px-2 py-0.5 text-[0.7rem] font-semibold text-white/70"
        >
          <span
            className="h-2 w-2 shrink-0 rounded-full"
            style={{ backgroundColor: role.color || '#8b8b8f' }}
          />
          {role.name}
        </span>
      ))}
    </span>
  );
}

type RoleToggleListProps = {
  guildId: string;
  member: HydratedGuildMember;
  roles: GuildRoleDto[];
  caller: RoleCaller;
  onChanged: () => void;
};

// Assignable-role checklist with hierarchy-aware gating; each toggle assigns or
// unassigns via the guild API. Shared by the members-panel popover and the
// profile card so the assignment rules live in one place.
export function RoleToggleList({ guildId, member, roles, caller, onChanged }: RoleToggleListProps) {
  const [pendingRoleIds, setPendingRoleIds] = useState<Set<string>>(new Set());
  const [error, setError] = useState('');
  const { pushToast } = useToast();

  useEffect(() => {
    if (error) {
      pushToast({
        title: 'Member roles',
        description: error,
        tone: 'error'
      });
    }
  }, [error, pushToast]);

  const assignableRoles = roles
    .filter((role) => !role.is_default)
    .sort((a, b) => b.position - a.position);
  const assignedRoleIds = new Set(member.roles.map((role) => role.id));

  async function handleToggle(role: GuildRoleDto, isAssigned: boolean) {
    setError('');
    setPendingRoleIds((prev) => new Set(prev).add(role.id));

    try {
      if (isAssigned) {
        await unassignGuildMemberRole(guildId, member.userId, role.id);
      } else {
        await assignGuildMemberRole(guildId, member.userId, role.id);
      }

      onChanged();
    } catch (toggleError) {
      setError(toggleError instanceof Error ? toggleError.message : 'Failed to update roles.');
    } finally {
      setPendingRoleIds((prev) => {
        const next = new Set(prev);
        next.delete(role.id);
        return next;
      });
    }
  }

  if (assignableRoles.length === 0) {
    return (
      <p className="px-1 pb-1 text-sm text-white/35">No roles yet. Create one in the Roles tab.</p>
    );
  }

  return (
    <ul className="grid max-h-64 gap-0.5 overflow-y-auto">
      {assignableRoles.map((role) => {
        const isAssigned = assignedRoleIds.has(role.id);
        const isPending = pendingRoleIds.has(role.id);
        const check = canToggleRole(role, isAssigned, caller);

        return (
          <li key={role.id}>
            <label
              title={check.reason ?? undefined}
              className={`flex items-center gap-2 rounded-md px-1.5 py-1 text-sm text-white/60 ${
                check.allowed ? 'cursor-pointer hover:bg-frame' : 'cursor-not-allowed opacity-50'
              }`}
            >
              <input
                type="checkbox"
                checked={isAssigned}
                disabled={!check.allowed || isPending}
                onChange={() => void handleToggle(role, isAssigned)}
                className="h-4 w-4 accent-[#78dce8]"
              />
              <span
                className="h-2.5 w-2.5 shrink-0 rounded-full"
                style={{ backgroundColor: role.color || '#8b8b8f' }}
              />
              <span className="min-w-0 flex-1 truncate">{role.name}</span>
            </label>
          </li>
        );
      })}
    </ul>
  );
}

type MemberRolesPopoverProps = {
  guildId: string;
  member: HydratedGuildMember;
  roles: GuildRoleDto[];
  caller: RoleCaller;
  /** Wrapper holding the trigger button and this popover; clicks outside it close the popover. */
  containerRef: RefObject<HTMLElement | null>;
  onChanged: () => void;
  onClose: () => void;
};

export function MemberRolesPopover({
  guildId,
  member,
  roles,
  caller,
  containerRef,
  onChanged,
  onClose
}: MemberRolesPopoverProps) {
  useCloseOnEscape(onClose);

  useEffect(() => {
    function handleMouseDown(event: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        onClose();
      }
    }

    document.addEventListener('mousedown', handleMouseDown);
    return () => document.removeEventListener('mousedown', handleMouseDown);
  }, [containerRef, onClose]);

  return (
    <div className="absolute right-0 top-9 z-20 w-64 rounded-md border border-stroke bg-panel p-2 shadow-lg">
      <div className="mb-1 flex items-center justify-between">
        <p className="px-1 text-xs font-bold uppercase tracking-wide text-white/40">Roles</p>
        <button
          type="button"
          onClick={onClose}
          className="flex h-6 w-6 items-center justify-center rounded-md text-[#8b8b8f] transition hover:bg-frame hover:text-white"
          aria-label="Close role list"
        >
          <X className="h-3.5 w-3.5" strokeWidth={2} />
        </button>
      </div>
      <RoleToggleList
        guildId={guildId}
        member={member}
        roles={roles}
        caller={caller}
        onChanged={onChanged}
      />
    </div>
  );
}
