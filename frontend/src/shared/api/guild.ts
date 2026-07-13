import { apiFetch } from './client';

export type GuildDto = {
  id: string;
  name: string;
  description?: string | null;
  icon_url?: string | null;
  banner_url?: string | null;
  owner_id: string;
  member_count?: number;
  created_at: string;
};

export type MyGuildDto = {
  id: string;
  name: string;
  icon_url?: string | null;
  owner_id: string;
  member_count?: number;
  joined_at: string;
};

export type GuildMemberDto = {
  user_id: string;
  nickname?: string | null;
  roles: string[];
  joined_at: string;
};

export type GuildMemberUpdateDto = {
  user_id: string;
  guild_id: string;
  nickname: string | null;
};

export type GuildBanDto = {
  user_id: string;
  banned_by: string | null;
  reason?: string | null;
  banned_at: string;
};

export type GuildChannelDto = {
  id: string;
  guild_id: string;
  name: string;
  type: 'text' | 'announcement' | 'voice';
  position: number;
  category_id?: string | null;
  topic?: string | null;
  is_nsfw?: boolean;
  slowmode_seconds?: number;
};

export type CreateChannelOverwriteInput = {
  target_id: string;
  target_type: ChannelOverwriteTargetType;
  allow: number;
  deny: number;
};

export type CreateChannelPayload = {
  name: string;
  type: 'text' | 'announcement';
  category_id?: string | null;
  position?: number;
  topic?: string | null;
  is_nsfw?: boolean;
  slowmode_seconds?: number;
  overwrites?: CreateChannelOverwriteInput[];
};

export type UpdateChannelPayload = Partial<CreateChannelPayload>;

export type GuildCategoryDto = {
  id: string;
  guild_id: string;
  name: string;
  position: number;
};

export type GuildRoleDto = {
  id: string;
  guild_id: string;
  name: string;
  color: string;
  permissions: string;
  position: number;
  is_hoisted: boolean;
  is_mentionable: boolean;
  is_default: boolean;
};

export type CreateGuildPayload = {
  name: string;
  description?: string;
  icon_url?: string;
  banner_url?: string;
};

export type UpdateGuildPayload = Partial<CreateGuildPayload>;

export type CreateInvitePayload = {
  max_uses?: number;
  expires_in_hours?: number;
};

export type GuildInviteDto = {
  code: string;
  guild_id: string;
  created_by: string;
  max_uses?: number | null;
  uses: number;
  expires_at?: string | null;
  created_at?: string;
};

export type InvitePreviewDto = {
  code: string;
  guild: {
    id: string;
    name: string;
    icon_url?: string | null;
    member_count?: number;
  };
  inviter: {
    id: string;
    username: string;
  };
  expires_at: string | null;
};

export type CreateRolePayload = {
  name: string;
  color?: string;
  permissions?: number;
  is_hoisted?: boolean;
  is_mentionable?: boolean;
};

export type UpdateRolePayload = Partial<CreateRolePayload>;

export type ChannelOverwriteTargetType = 'role' | 'user';

export type ChannelOverwriteDto = {
  target_id: string;
  target_type: ChannelOverwriteTargetType;
  allow: number;
  deny: number;
};

export type PutChannelOverwritePayload = {
  target_type: ChannelOverwriteTargetType;
  allow: number;
  deny: number;
};

export type CreateCategoryPayload = {
  name: string;
  position?: number;
};

export type UpdateCategoryPayload = Partial<CreateCategoryPayload>;

export function listMyGuilds() {
  return apiFetch<MyGuildDto[]>('guild', '/guilds/me');
}

export function createGuild(payload: CreateGuildPayload) {
  return apiFetch<GuildDto>('guild', '/guilds', {
    method: 'POST',
    body: payload
  });
}

export function getGuild(guildId: string) {
  return apiFetch<GuildDto>('guild', `/guilds/${guildId}`);
}

export function updateGuild(guildId: string, payload: UpdateGuildPayload) {
  return apiFetch<GuildDto>('guild', `/guilds/${guildId}`, {
    method: 'PATCH',
    body: payload
  });
}

export function deleteGuild(guildId: string) {
  return apiFetch<void>('guild', `/guilds/${guildId}`, {
    method: 'DELETE'
  });
}

// hand the guild over to another member; the current owner becomes a regular
// member. owner-only, irreversible without the new owner transferring it back.
export function transferGuildOwnership(guildId: string, newOwnerId: string) {
  return apiFetch<GuildDto>('guild', `/guilds/${guildId}/owner`, {
    method: 'PATCH',
    body: { new_owner_id: newOwnerId }
  });
}

export function joinGuild(guildId: string, inviteCode: string) {
  return apiFetch<GuildDto>('guild', `/guilds/${guildId}/join`, {
    method: 'POST',
    body: { invite_code: inviteCode }
  });
}

export function leaveGuild(guildId: string) {
  return apiFetch<void>('guild', `/guilds/${guildId}/leave`, {
    method: 'POST'
  });
}

export function listGuildMembers(guildId: string, query?: { limit?: number; after?: string }) {
  return apiFetch<GuildMemberDto[]>('guild', `/guilds/${guildId}/members`, { query });
}

export function updateGuildMemberNickname(
  guildId: string,
  userId: string,
  nickname: string | null
) {
  return apiFetch<GuildMemberUpdateDto>('guild', `/guilds/${guildId}/members/${userId}`, {
    method: 'PATCH',
    body: { nickname }
  });
}

export function kickGuildMember(guildId: string, userId: string) {
  return apiFetch<void>('guild', `/guilds/${guildId}/members/${userId}`, {
    method: 'DELETE'
  });
}

export function assignGuildMemberRole(guildId: string, userId: string, roleId: string) {
  return apiFetch<void>('guild', `/guilds/${guildId}/members/${userId}/roles/${roleId}`, {
    method: 'PUT'
  });
}

export function unassignGuildMemberRole(guildId: string, userId: string, roleId: string) {
  return apiFetch<void>('guild', `/guilds/${guildId}/members/${userId}/roles/${roleId}`, {
    method: 'DELETE'
  });
}

export function listGuildBans(guildId: string, query?: { limit?: number; after?: string }) {
  return apiFetch<GuildBanDto[]>('guild', `/guilds/${guildId}/bans`, { query });
}

export function banGuildMember(guildId: string, userId: string, reason?: string) {
  return apiFetch<void>('guild', `/guilds/${guildId}/bans/${userId}`, {
    method: 'POST',
    body: reason ? { reason } : {}
  });
}

export function unbanGuildMember(guildId: string, userId: string) {
  return apiFetch<void>('guild', `/guilds/${guildId}/bans/${userId}`, {
    method: 'DELETE'
  });
}

export function listGuildChannels(guildId: string) {
  return apiFetch<GuildChannelDto[]>('guild', `/guilds/${guildId}/channels`);
}

// snowflake ids of the members who can READ the channel; used to scope the
// guild member list to who can actually see the currently open channel.
export async function listChannelReaders(guildId: string, channelId: string) {
  const { user_ids } = await apiFetch<{ user_ids: string[] }>(
    'guild',
    `/guilds/${guildId}/channels/${channelId}/members`
  );
  return user_ids;
}

export function createGuildChannel(guildId: string, payload: CreateChannelPayload) {
  return apiFetch<GuildChannelDto>('guild', `/guilds/${guildId}/channels`, {
    method: 'POST',
    body: payload
  });
}

export function updateGuildChannel(
  guildId: string,
  channelId: string,
  payload: UpdateChannelPayload
) {
  return apiFetch<GuildChannelDto>('guild', `/guilds/${guildId}/channels/${channelId}`, {
    method: 'PATCH',
    body: payload
  });
}

export function deleteGuildChannel(guildId: string, channelId: string) {
  return apiFetch<void>('guild', `/guilds/${guildId}/channels/${channelId}`, {
    method: 'DELETE'
  });
}

export function listChannelPermissionOverwrites(channelId: string) {
  return apiFetch<ChannelOverwriteDto[]>('guild', `/channels/${channelId}/permissions`);
}

export function putChannelPermissionOverwrite(
  channelId: string,
  targetId: string,
  payload: PutChannelOverwritePayload
) {
  return apiFetch<void>('guild', `/channels/${channelId}/permissions/${targetId}`, {
    method: 'PUT',
    body: payload
  });
}

export function deleteChannelPermissionOverwrite(channelId: string, targetId: string) {
  return apiFetch<void>('guild', `/channels/${channelId}/permissions/${targetId}`, {
    method: 'DELETE'
  });
}

export function listGuildCategories(guildId: string) {
  return apiFetch<GuildCategoryDto[]>('guild', `/guilds/${guildId}/categories`);
}

export function createGuildCategory(guildId: string, payload: CreateCategoryPayload) {
  return apiFetch<GuildCategoryDto>('guild', `/guilds/${guildId}/categories`, {
    method: 'POST',
    body: payload
  });
}

export function updateGuildCategory(
  guildId: string,
  categoryId: string,
  payload: UpdateCategoryPayload
) {
  return apiFetch<GuildCategoryDto>('guild', `/guilds/${guildId}/categories/${categoryId}`, {
    method: 'PATCH',
    body: payload
  });
}

export function deleteGuildCategory(guildId: string, categoryId: string) {
  return apiFetch<void>('guild', `/guilds/${guildId}/categories/${categoryId}`, {
    method: 'DELETE'
  });
}

export function listGuildRoles(guildId: string) {
  return apiFetch<GuildRoleDto[]>('guild', `/guilds/${guildId}/roles`);
}

export function createGuildRole(guildId: string, payload: CreateRolePayload) {
  return apiFetch<GuildRoleDto>('guild', `/guilds/${guildId}/roles`, {
    method: 'POST',
    body: payload
  });
}

export function updateGuildRole(guildId: string, roleId: string, payload: UpdateRolePayload) {
  return apiFetch<GuildRoleDto>('guild', `/guilds/${guildId}/roles/${roleId}`, {
    method: 'PATCH',
    body: payload
  });
}

export function deleteGuildRole(guildId: string, roleId: string) {
  return apiFetch<void>('guild', `/guilds/${guildId}/roles/${roleId}`, {
    method: 'DELETE'
  });
}

export type ReorderRoleEntry = { id: string; position: number };

// bulk reorder: `moves` must cover every non-@everyone role, and their target
// positions must be a permutation of the positions those roles currently hold.
// returns the full, freshly-ordered role list.
export function reorderGuildRoles(guildId: string, moves: ReorderRoleEntry[]) {
  return apiFetch<GuildRoleDto[]>('guild', `/guilds/${guildId}/roles`, {
    method: 'PATCH',
    body: moves
  });
}

export function createGuildInvite(guildId: string, payload: CreateInvitePayload = {}) {
  return apiFetch<GuildInviteDto>('guild', `/guilds/${guildId}/invites`, {
    method: 'POST',
    body: payload
  });
}

export function listGuildInvites(guildId: string) {
  return apiFetch<GuildInviteDto[]>('guild', `/guilds/${guildId}/invites`);
}

export function revokeGuildInvite(guildId: string, code: string) {
  return apiFetch<void>('guild', `/guilds/${guildId}/invites/${code}`, {
    method: 'DELETE'
  });
}

export function previewInvite(code: string) {
  return apiFetch<InvitePreviewDto>('guild', `/invites/${code}`);
}
