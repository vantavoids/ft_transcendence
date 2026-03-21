# API Contract - Guild Service

Base path: `/guilds`

All responses are `application/json`. Errors follow the shape:
```json
{ "error": "human-readable message" }
```

All endpoints require `Authorization: Bearer <access_token>`.

---

## Guild Endpoints

### POST /guilds

Create a new guild. The creator becomes the owner and gets the `Administrator` role.

**Request body:**
```json
{
  "name": "le transcendage",
  "description": "viens on refait minishell mais en web",
  "icon_url": "https://..."
}
```

`name` is required (1–100 chars). All other fields optional.

**Response `201`:**
```json
{
  "id": "<uuid>",
  "name": "Zig Programming Language",
  "description": "parce que zig > rust :D",
  "icon_url": "https://...",
  "owner_id": "<uuid>",
  "created_at": "2026-03-09T00:00:00Z"
}
```

---

### GET /guilds/{id}

Get guild details. Caller must be a member.

**Response `200`:**
```json
{
  "id": "<uuid>",
  "name": "e-girls bar",
  "description": "shhh",
  "icon_url": "https://...",
  "owner_id": "<uuid>",
  "member_count": 42,
  "created_at": "2026-03-09T00:00:00Z"
}
```

**Errors:**
| Status | Reason |
|--------|--------|
| 403 | Not a member |
| 404 | Guild not found |

---

### PATCH /guilds/{id}

Update guild settings. Requires `MANAGE_GUILD` permission.

**Request body** (all fields optional):
```json
{
  "name": "42 sh",
  "description": "84 sh would've been more fitting...",
  "icon_url": "https://..."
}
```

**Response `200`:** Updated guild object.

**Errors:**
| Status | Reason |
|--------|--------|
| 403 | Missing `MANAGE_GUILD` permission |
| 404 | Guild not found |

---

### DELETE /guilds/{id}

Delete a guild. Owner only.

**Response `204`:** No content.

**Errors:**
| Status | Reason |
|--------|--------|
| 403 | Not the owner |
| 404 | Guild not found |

---

## Membership Endpoints

### POST /guilds/{id}/join

Join a guild via invite code.

**Request body:**
```json
{
  "invite_code": "invite_code"
}
```

**Response `200`:** Guild object.

**Errors:**
| Status | Reason |
|--------|--------|
| 400 | Invalid or expired invite code |
| 409 | Already a member |

**Side effects:**
- Validates user exists via HTTP GET to User Service
- Publishes `guild.member_joined { guild_id, user_id }` to RabbitMQ

---

### POST /guilds/{id}/leave

Leave a guild. Owner cannot leave (must transfer ownership or delete).

**Response `204`:** No content.

**Errors:**
| Status | Reason |
|--------|--------|
| 400 | Owner cannot leave |
| 403 | Not a member |
| 404 | Guild not found |

**Side effects:** Publishes `guild.member_left { guild_id, user_id }` to RabbitMQ.

---

### GET /guilds/{id}/members

List guild members. Caller must be a member.

**Query params:**
| Param | Type | Description |
|-------|------|-------------|
| `limit` | int | Max results (default 50, max 100) |
| `after` | uuid | Cursor for pagination |

**Response `200`:**
```json
[
  {
    "user_id": "<uuid>",
    "nickname": "Vanta",
    "roles": ["<uuid>"],
    "joined_at": "2026-03-09T00:00:00Z"
  }
]
```

---

### DELETE /guilds/{id}/members/{user_id}

Kick a member. Requires `KICK_MEMBERS` permission. Cannot kick the owner or someone with equal/higher role.

**Response `204`:** No content.

**Errors:**
| Status | Reason |
|--------|--------|
| 403 | Missing permission or target has higher/equal role |
| 404 | Guild or member not found |

**Side effects:** Publishes `guild.member_left { guild_id, user_id }` to RabbitMQ.

---

### GET /guilds/{id}/bans

List banned users. Requires `BAN_MEMBERS` permission.

**Response `200`:**
```json
[
  {
    "user_id": "<uuid>",
    "reason": "il a segfault sur des double quotes le con",
    "banned_at": "2026-03-09T00:00:00Z"
  }
]
```

---

### POST /guilds/{id}/bans/{user_id}

Ban a member. Requires `BAN_MEMBERS` permission.

**Request body** (optional):
```json
{
  "reason": "il a segfault sur des double quotes le con"
}
```

**Response `204`:** No content.

**Errors:**
| Status | Reason |
|--------|--------|
| 403 | Missing permission or target has higher/equal role |
| 404 | Guild or user not found |
| 409 | Already banned |

---

### DELETE /guilds/{id}/bans/{user_id}

Unban a user. Requires `BAN_MEMBERS` permission.

**Response `204`:** No content.

---

## Invite Endpoints

### POST /guilds/{id}/invites

Create an invite link. Requires `CREATE_INVITE` permission.

**Request body** (optional):
```json
{
  "max_uses": 10,
  "expires_in_hours": 24
}
```

Omit for unlimited uses / never expires.

**Response `201`:**
```json
{
  "code": "invite_code",
  "guild_id": "<uuid>",
  "created_by": "<uuid>",
  "max_uses": 10,
  "uses": 0,
  "expires_at": "2026-03-10T00:00:00Z"
}
```

**Side effects:** Publishes `guild.invite_created { guild_id, guild_name, invited_by_user_id }` to RabbitMQ when used to invite a specific user (optional targeted invite).

---

## Channel Endpoints

### GET /guilds/{id}/channels

List all channels in a guild. Caller must be a member.

**Response `200`:**
```json
[
  {
    "id": "<uuid>",
    "guild_id": "<uuid>",
    "name": "general",
    "type": "text",
    "position": 0
  }
]
```

---

### POST /guilds/{id}/channels

Create a channel. Requires `MANAGE_CHANNELS` permission.

**Request body:**
```json
{
  "name": "general",
  "type": "text",
  "category_id": "<uuid>",
  "position": 0,
  "topic": "general chat",
  "is_nsfw": false,
  "slowmode_seconds": 0
}
```

`type` must be `text`, `announcement`, or `voice`. `category_id` is optional (null = uncategorised).

**Response `201`:** Channel object.

**Errors:**
| Status | Reason |
|--------|--------|
| 400 | Invalid name or type |
| 403 | Missing `MANAGE_CHANNELS` permission |

---

### PATCH /guilds/{id}/channels/{channel_id}

Update a channel. Requires `MANAGE_CHANNELS` permission.

**Request body** (all optional):
```json
{
  "name": "new-name",
  "position": 2
}
```

**Response `200`:** Updated channel object.

---

### DELETE /guilds/{id}/channels/{channel_id}

Delete a channel. Requires `MANAGE_CHANNELS` permission.

**Response `204`:** No content.

---

## Channel Category Endpoints

### POST /guilds/{id}/categories

Create a channel category. Requires `MANAGE_CHANNELS` permission.

**Request body:**
```json
{
  "name": "Text Channels",
  "position": 0
}
```

**Response `201`:** Category object `{ id, guild_id, name, position }`.

---

### PATCH /guilds/{id}/categories/{category_id}

Update a category name or position. Requires `MANAGE_CHANNELS` permission.

**Request body** (all optional):
```json
{
  "name": "Voice Channels",
  "position": 1
}
```

**Response `200`:** Updated category object.

---

### DELETE /guilds/{id}/categories/{category_id}

Delete a category. Channels in it become uncategorised. Requires `MANAGE_CHANNELS` permission.

**Response `204`:** No content.

---

## Ownership

### PATCH /guilds/{id}/owner

Transfer guild ownership to another member. Owner only.

**Request body:**
```json
{
  "new_owner_id": "<uuid>"
}
```

**Response `200`:** Updated guild object.

**Errors:**
| Status | Reason |
|--------|--------|
| 403 | Not the owner |
| 404 | Target user is not a member |

---

## Permission Bitmask

Each role has a `permissions` field - a `BIGINT` bitmask. Bit positions:

| Permission | Bit | Value |
|------------|-----|-------|
| `VIEW_CHANNELS` | 0 | `1` |
| `SEND_MESSAGES` | 1 | `2` |
| `MANAGE_MESSAGES` | 2 | `4` |
| `MANAGE_CHANNELS` | 3 | `8` |
| `KICK_MEMBERS` | 4 | `16` |
| `BAN_MEMBERS` | 5 | `32` |
| `CREATE_INVITE` | 6 | `64` |
| `MANAGE_ROLES` | 7 | `128` |
| `MANAGE_GUILD` | 8 | `256` |
| `ADMINISTRATOR` | 9 | `512` |

`ADMINISTRATOR` grants all permissions regardless of other bits. The guild owner always has `ADMINISTRATOR`.

---

## Role Endpoints

### GET /guilds/{id}/roles

List all roles in a guild. Caller must be a member. Returns roles in priority order (position ascending - higher position = lower priority).

**Response `200`:**
```json
[
  {
    "id": "<uuid>",
    "guild_id": "<uuid>",
    "name": "Moderator",
    "color": "#FF5733",
    "permissions": 52,
    "position": 1,
    "is_hoisted": true,
    "is_mentionable": false,
    "is_default": false
  }
]
```

---

### POST /guilds/{id}/roles

Create a new role. Requires `MANAGE_ROLES` permission.

**Request body:**
```json
{
  "name": "Moderator",
  "color": "#FF5733",
  "permissions": 52,
  "is_hoisted": true,
  "is_mentionable": false
}
```

- `name`: required, 1–100 chars
- `permissions`: bitmask (default `0`). Cannot grant permissions the caller doesn't have.

**Response `201`:** Role object.

**Errors:**
| Status | Reason |
|--------|--------|
| 400 | Invalid name, color, or permissions value |
| 403 | Missing `MANAGE_ROLES` or attempting to grant permissions caller lacks |

---

### PATCH /guilds/{id}/roles/{role_id}

Update a role. Requires `MANAGE_ROLES` permission. Cannot grant permissions the caller doesn't have.

**Request body** (all optional):
```json
{
  "name": "Senior Moderator",
  "color": "#3498DB",
  "permissions": 60,
  "position": 2,
  "is_hoisted": true,
  "is_mentionable": true
}
```

**Response `200`:** Updated role object.

**Errors:**
| Status | Reason |
|--------|--------|
| 403 | Missing `MANAGE_ROLES` or attempting to grant permissions caller lacks |
| 404 | Role not found |

---

### DELETE /guilds/{id}/roles/{role_id}

Delete a role. Requires `MANAGE_ROLES` permission. Cannot delete the default `@everyone` role.

**Response `204`:** No content.

**Errors:**
| Status | Reason |
|--------|--------|
| 400 | Cannot delete the default role |
| 403 | Missing `MANAGE_ROLES` |
| 404 | Role not found |

---

## Role Assignment Endpoints

### GET /guilds/{id}/members/{user_id}/permissions

Get the effective permission bitmask for a user in a guild - resolves all their roles, channel overwrites are not applied here (use the internal endpoint for that).

**Response `200`:**
```json
{
  "user_id": "<uuid>",
  "guild_id": "<uuid>",
  "roles": [
    { "id": "<uuid>", "name": "Moderator", "permissions": 52 }
  ],
  "effective_permissions": 52,
  "is_owner": false
}
```

**Errors:**
| Status | Reason |
|--------|--------|
| 404 | User is not a member of this guild |

---

### PUT /guilds/{id}/members/{user_id}/roles/{role_id}

Assign a role to a guild member. Requires `MANAGE_ROLES`. Cannot assign a role with permissions the caller doesn't have.

**Response `204`:** No content.

**Errors:**
| Status | Reason |
|--------|--------|
| 403 | Missing `MANAGE_ROLES` or role has higher permissions than caller |
| 404 | User not a member, or role not found in this guild |

---

### DELETE /guilds/{id}/members/{user_id}/roles/{role_id}

Remove a role from a guild member. Requires `MANAGE_ROLES`. Cannot remove the default `@everyone` role.

**Response `204`:** No content.

**Errors:**
| Status | Reason |
|--------|--------|
| 403 | Missing `MANAGE_ROLES` |
| 404 | User, role, or assignment not found |

---

## Channel Permission Overwrites

Per-channel allow/deny overrides for a specific role or user.

### PUT /channels/{channel_id}/permissions/{target_id}

Create or update a permission overwrite. Requires `MANAGE_CHANNELS`.

**Request body:**
```json
{
  "target_type": "role",
  "allow": 2,
  "deny": 0
}
```

`target_type`: `"role"` or `"user"`. `target_id` is the role or user UUID.

**Response `204`:** No content.

---

### DELETE /channels/{channel_id}/permissions/{target_id}

Delete a permission overwrite. Requires `MANAGE_CHANNELS`.

**Response `204`:** No content.

---

## Internal Endpoint (used by Chat Service)

### GET /channels/{channel_id}/membership

Check if a user is a member of the guild that owns this channel, and get their effective permissions. Called by Chat Service over internal HTTP before allowing a message to be sent.

**Auth required:** Internal service token (not user JWT)

**Query params:**
| Param | Type | Description |
|-------|------|-------------|
| `user_id` | uuid | The user to check |

**Response `200`:**
```json
{
  "is_member": true,
  "permissions": 2147483647
}
```

`permissions` is a bitmask. Chat Service checks the `SEND_MESSAGES` bit before accepting the message.

**Errors:**
| Status | Reason |
|--------|--------|
| 404 | Channel or user not found |

---

## RabbitMQ Events Published

| Event | Payload | Trigger |
|-------|---------|---------|
| `guild.member_joined` | `{ guild_id, user_id }` | User joins a guild |
| `guild.member_left` | `{ guild_id, user_id }` | User leaves, is kicked, or banned |
| `guild.invite_created` | `{ guild_id, guild_name, invited_by_user_id, invited_user_id? }` | Targeted invite sent |

