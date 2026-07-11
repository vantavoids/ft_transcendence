'use client';

import { useCallback, useEffect, useState } from 'react';
import {
  listGuildMembers,
  listGuildRoles,
  type GuildMemberDto,
  type GuildRoleDto
} from '../api/guild';
import { getUser, getUsersByIds, type UserStatus, type UserSummaryDto } from '../api/user';
import { subscribeProfileUpdated } from '../lib/profile-events';

const MEMBERS_PAGE_SIZE = 100;
const MEMBERS_MAX_PAGES = 10;

export type HydratedGuildMember = {
  userId: string;
  /** Nickname when set, otherwise the user's display name. */
  displayName: string;
  username: string | null;
  avatarUrl: string | null;
  bannerUrl: string | null;
  bio: string | null;
  nickname: string | null;
  status: UserStatus;
  roles: GuildRoleDto[];
  joinedAt: string;
  isOwner: boolean;
  isDeleted: boolean;
};

type HydrationInput = {
  memberRows: GuildMemberDto[];
  guildRoles: GuildRoleDto[];
  users: UserSummaryDto[];
  deletedUserIds?: Set<string>;
  ownerId?: string | null;
};

async function fetchAllMembers(guildId: string) {
  const members: GuildMemberDto[] = [];
  let after: string | undefined;

  for (let page = 0; page < MEMBERS_MAX_PAGES; page += 1) {
    const batch = await listGuildMembers(guildId, { limit: MEMBERS_PAGE_SIZE, after });
    members.push(...batch);

    if (batch.length < MEMBERS_PAGE_SIZE) {
      break;
    }

    after = batch[batch.length - 1].user_id;
  }

  return members;
}

async function fetchMemberUsers(memberRows: GuildMemberDto[]) {
  const batchUsers = await getUsersByIds(memberRows.map((member) => member.user_id)).catch(
    () => []
  );
  const usersById = new Map(batchUsers.map((user) => [user.id, user]));
  const missingIds = memberRows
    .map((member) => member.user_id)
    .filter((userId) => !usersById.has(userId));

  const fallbackUsers = await Promise.all(
    missingIds.map(async (userId) => {
      try {
        return await getUser(userId);
      } catch {
        return null;
      }
    })
  );

  for (const user of fallbackUsers) {
    if (user) {
      usersById.set(user.id, user);
    }
  }

  return {
    users: Array.from(usersById.values()),
    deletedUserIds: new Set(missingIds.filter((userId) => !usersById.has(userId)))
  };
}

export function hydrateGuildMembers({
  memberRows,
  guildRoles,
  users,
  deletedUserIds = new Set(),
  ownerId = null
}: HydrationInput): HydratedGuildMember[] {
  const usersById = new Map(users.map((user) => [user.id, user]));
  const rolesById = new Map(guildRoles.map((role) => [role.id, role]));

  return memberRows.map<HydratedGuildMember>((member) => {
    const user = usersById.get(member.user_id);
    const memberRoles = member.roles
      .map((roleId) => rolesById.get(roleId))
      .filter((role): role is GuildRoleDto => Boolean(role))
      .sort((a, b) => b.position - a.position);

    const isDeleted = deletedUserIds.has(member.user_id) && !user;

    return {
      userId: member.user_id,
      displayName: member.nickname ?? user?.display_name ?? (isDeleted ? 'Deleted User' : 'Member'),
      username: user?.username ?? null,
      avatarUrl: user?.avatar_url ?? null,
      bannerUrl: user?.banner_url ?? null,
      bio: user?.bio ?? null,
      nickname: member.nickname ?? null,
      status: user?.status ?? 'offline',
      roles: memberRoles,
      joinedAt: member.joined_at,
      isOwner: Boolean(ownerId) && member.user_id === ownerId,
      isDeleted
    };
  });
}

export function useGuildMembers(guildId: string | null, ownerId?: string | null) {
  const [members, setMembers] = useState<HydratedGuildMember[]>([]);
  const [roles, setRoles] = useState<GuildRoleDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    if (!guildId) {
      setMembers([]);
      setRoles([]);
      return;
    }

    setIsLoading(true);
    setError('');

    try {
      const [memberRows, guildRoles] = await Promise.all([
        fetchAllMembers(guildId),
        listGuildRoles(guildId)
      ]);
      const { users, deletedUserIds } = await fetchMemberUsers(memberRows);
      const hydrated = hydrateGuildMembers({
        memberRows,
        guildRoles,
        users,
        deletedUserIds,
        ownerId
      });

      setRoles(guildRoles);
      setMembers(hydrated);
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : 'Failed to load members.');
    } finally {
      setIsLoading(false);
    }
  }, [guildId, ownerId]);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    return subscribeProfileUpdated(() => {
      void load();
    });
  }, [load]);

  return { members, roles, isLoading, error, refresh: load };
}
