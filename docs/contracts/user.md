# API Contract - User Service

Base path: `/users`

All responses are `application/json`. Errors follow the shape:
```json
{ "error": "human-readable message" }
```

All endpoints require `Authorization: Bearer <access_token>` unless noted.

---

## User Endpoints

### GET /users/me

Get the authenticated user's own profile.

**Response `200`:**
```json
{
  "id": "<uuid>",
  "username": "spuffoon238",
  "display_name": "Spuffie",
  "avatar_url": "https://...",
  "banner_url": "https://...",
  "status": "online",
  "bio": "...",
  "last_seen_at": "2026-03-09T12:00:00Z"
}
```

---

### GET /users/{id}

Get a user's public profile by ID.

**Response `200`:**
```json
{
  "id": "<uuid>",
  "username": "skaf_angel",
  "display_name": "Skaf",
  "avatar_url": "https://...",
  "banner_url": "https://...",
  "status": "offline",
  "bio": "...",
  "last_seen_at": "2026-03-09T11:30:00Z"
}
```

**Errors:**
| Status | Reason |
|--------|--------|
| 404 | User not found |

---

### PATCH /users/{id}

Update own profile. Can only update your own profile (`{id}` must match JWT `sub`).

**Request body** (all fields optional):
```json
{
  "display_name": "yandry",
  "bio": "segfault eater",
  "status": "idle"
}
```

`status` must be one of: `online`, `idle`, `dnd`, `offline`

**Response `200`:** Updated profile object (same shape as GET).

**Errors:**
| Status | Reason |
|--------|--------|
| 400 | Invalid field values |
| 403 | Trying to update another user's profile |
| 404 | User not found |

---

### POST /users/{id}/avatar

Upload a new avatar image. Multipart form data.

**Request:** `Content-Type: multipart/form-data`
- Field `avatar`: image file (JPEG/PNG/WebP, max 5MB)

**Response `200`:**
```json
{
  "avatar_url": "https://..."
}
```

**Errors:**
| Status | Reason |
|--------|--------|
| 400 | Invalid file type or size exceeded |
| 403 | Not your profile |

---

## Friend Endpoints

### GET /users/{id}/friends

List a user's friends.

**Response `200`:**
```json
[
  {
    "id": "<uuid>",
    "username": "tstephan",
    "display_name": "SkyDogzz",
    "avatar_url": "https://...",
    "status": "online",
    "friendship_status": "accepted"
  }
]
```

---

### POST /friends

Send a friend request.

**Request body:**
```json
{
  "addressee_id": "<uuid>"
}
```

**Response `201`:**
```json
{
  "id": "<uuid>",
  "requester_id": "<uuid>",
  "addressee_id": "<uuid>",
  "status": "pending",
  "created_at": "2026-03-09T00:00:00Z"
}
```

**Errors:**
| Status | Reason |
|--------|--------|
| 400 | Cannot send request to yourself |
| 404 | Addressee not found |
| 409 | Request already exists or users are already friends |

**Side effects:** Publishes `friend.request_sent { from_user_id, to_user_id }` to RabbitMQ.

---

### PATCH /friends/{id}

Accept, decline, or block a friend request.

**Request body:**
```json
{
  "status": "accepted"
}
```

`status` must be one of: `accepted`, `blocked`

To decline, use DELETE instead.

**Response `200`:** Updated friendship object.

**Errors:**
| Status | Reason |
|--------|--------|
| 400 | Invalid status value |
| 403 | Not the addressee of this request |
| 404 | Friendship not found |

---

### DELETE /friends/{id}

Remove a friend or decline/cancel a friend request.

**Response `204`:** No content.

**Errors:**
| Status | Reason |
|--------|--------|
| 403 | Not a participant in this friendship |
| 404 | Friendship not found |

---

## Block Endpoints

### POST /users/{id}/block

Block a user. This is unilateral - the blocked user is not notified.

**Response `204`:** No content.

**Errors:**
| Status | Reason |
|--------|--------|
| 400 | Cannot block yourself |
| 404 | User not found |
| 409 | Already blocking this user |

---

### DELETE /users/{id}/block

Unblock a user.

**Response `204`:** No content.

**Errors:**
| Status | Reason |
|--------|--------|
| 404 | Block not found |

---

### GET /users/me/blocks

List all users the authenticated user has blocked.

**Response `200`:**
```json
[
  {
    "user_id": "<uuid>",
    "username": "annoying_user",
    "blocked_at": "2026-03-09T00:00:00Z"
  }
]
```

---

## RabbitMQ Events Published

| Event | Payload | Trigger |
|-------|---------|---------|
| `friend.request_sent` | `{ from_user_id, to_user_id }` | Friend request sent |

## RabbitMQ Events Consumed

| Event | Action |
|-------|--------|
| `user.registered` | Create profile row with defaults (username = email prefix, status = offline) |
| `user.online` | Update `status = online` |
| `user.offline` | Update `status = offline` |

