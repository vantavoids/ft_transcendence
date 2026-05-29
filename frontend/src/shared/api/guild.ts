import { apiFetch } from './client';

export type GuildDto = {
  id: string;
  name: string;
  description?: string | null;
  icon_url?: string | null;
  owner_id: string;
  member_count?: number;
  created_at: string;
};

export type GuildMemberDto = {
  user_id: string;
  nickname?: string | null;
  roles: string[];
  joined_at: string;
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
};

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

export function listGuildMembers(
  guildId: string,
  query?: { limit?: number; after?: string }
) {
  return apiFetch<GuildMemberDto[]>('guild', `/guilds/${guildId}/members`, { query });
}

export function listGuildChannels(guildId: string) {
  return apiFetch<GuildChannelDto[]>('guild', `/guilds/${guildId}/channels`);
}

export function listGuildRoles(guildId: string) {
  return apiFetch<GuildRoleDto[]>('guild', `/guilds/${guildId}/roles`);
}

export function createGuildInvite(guildId: string, payload: CreateInvitePayload = {}) {
  return apiFetch<GuildInviteDto>('guild', `/guilds/${guildId}/invites`, {
    method: 'POST',
    body: payload
  });
}
