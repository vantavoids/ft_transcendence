'use client';

import { useCallback, useEffect, useState } from 'react';
import {
  listGuildMembers,
  listGuildRoles,
  type GuildMemberDto,
  type GuildRoleDto
} from '../api/guild';
import { getUsersByIds, type UserStatus } from '../api/user';

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
      const users = await getUsersByIds(memberRows.map((member) => member.user_id));
      const usersById = new Map(users.map((user) => [user.id, user]));
      const rolesById = new Map(guildRoles.map((role) => [role.id, role]));

      const hydrated = memberRows.map<HydratedGuildMember>((member) => {
        const user = usersById.get(member.user_id);
        const memberRoles = member.roles
          .map((roleId) => rolesById.get(roleId))
          .filter((role): role is GuildRoleDto => Boolean(role))
          .sort((a, b) => b.position - a.position);

        return {
          userId: member.user_id,
          displayName: member.nickname ?? user?.display_name ?? 'Deleted User',
          username: user?.username ?? null,
          avatarUrl: user?.avatar_url ?? null,
          bannerUrl: user?.banner_url ?? null,
          bio: user?.bio ?? null,
          nickname: member.nickname ?? null,
          status: user?.status ?? 'offline',
          roles: memberRoles,
          joinedAt: member.joined_at,
          isOwner: Boolean(ownerId) && member.user_id === ownerId,
          isDeleted: !user
        };
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

  return { members, roles, isLoading, error, refresh: load };
}
