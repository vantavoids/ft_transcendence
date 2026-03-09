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
    "role_id": "<uuid>",
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
  "position": 0
}
```

`type` must be `text` or `announcement`.

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

