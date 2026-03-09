# API Contract - Permissions Service

Base path: `/guilds/{guild_id}`

All responses are `application/json`. Errors follow the shape:
```json
{ "error": "human-readable message" }
```

All endpoints require `Authorization: Bearer <access_token>`.

The Permissions Service manages roles and their permission bitmasks. Role assignment to guild members lives here. The Guild Service delegates permission checks to this service (or checks the bitmask it already stores - see note below).

> **Note:** The `guild_members.role_id` and `roles.permissions` columns live in the Guild Service's DB. The Permissions Service is the authority for role CRUD and assignment. For the permission check on message send, Chat Service calls Guild Service (which has the bitmask), not Permissions Service directly. Permissions Service is called for role management operations.

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

`ADMINISTRATOR` grants all permissions regardless of other bits.

---

## Role Endpoints

### GET /guilds/{guild_id}/roles

List all roles in a guild. Caller must be a member.

**Response `200`:**
```json
[
  {
    "id": "<uuid>",
    "guild_id": "<uuid>",
    "name": "Moderator",
    "color": "#FF5733",
    "permissions": 52,
    "created_at": "2026-03-09T00:00:00Z"
  }
]
```

Roles are returned in priority order (higher index = lower priority).

---

### POST /guilds/{guild_id}/roles

Create a new role. Requires `MANAGE_ROLES` permission.

**Request body:**
```json
{
  "name": "Moderator",
  "color": "#FF5733",
  "permissions": 52
}
```

- `name`: required, 1–100 chars
- `color`: optional, hex color string
- `permissions`: optional bitmask (default `0` - no permissions)

**Response `201`:** Role object.

**Errors:**
| Status | Reason |
|--------|--------|
| 400 | Invalid name, color, or permissions value |
| 403 | Missing `MANAGE_ROLES` permission |

---

### PATCH /guilds/{guild_id}/roles/{role_id}

Update a role. Requires `MANAGE_ROLES` permission. Cannot grant permissions the caller doesn't have.

**Request body** (all optional):
```json
{
  "name": "Senior Moderator",
  "color": "#3498DB",
  "permissions": 60
}
```

**Response `200`:** Updated role object.

**Errors:**
| Status | Reason |
|--------|--------|
| 400 | Invalid fields |
| 403 | Missing `MANAGE_ROLES` or attempting to grant permissions caller lacks |
| 404 | Role not found |

---

### DELETE /guilds/{guild_id}/roles/{role_id}

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

### GET /guilds/{guild_id}/members/{user_id}/permissions

Get the effective permission bitmask for a user in a guild (their role's permissions, or `ADMINISTRATOR` if owner).

**Response `200`:**
```json
{
  "user_id": "<uuid>",
  "guild_id": "<uuid>",
  "role_id": "<uuid>",
  "role_name": "Moderator",
  "permissions": 52,
  "is_owner": false
}
```

**Errors:**
| Status | Reason |
|--------|--------|
| 404 | User is not a member of this guild |

---

### PUT /guilds/{guild_id}/members/{user_id}/role

Assign a role to a guild member. Requires `MANAGE_ROLES` permission. Cannot assign a role with permissions the caller doesn't have. Cannot reassign the owner's role.

**Request body:**
```json
{
  "role_id": "<uuid>"
}
```

**Response `200`:**
```json
{
  "user_id": "<uuid>",
  "role_id": "<uuid>",
  "updated_at": "2026-03-09T12:00:00Z"
}
```

**Errors:**
| Status | Reason |
|--------|--------|
| 403 | Missing `MANAGE_ROLES` or role has higher permissions than caller |
| 404 | User not a member, or role not found in this guild |

