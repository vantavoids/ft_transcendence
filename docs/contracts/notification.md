# API Contract - Notification Service

Base path: `/notifications`

All responses are `application/json`. Errors follow the shape:
```json
{ "error": "human-readable message" }
```

All endpoints require `Authorization: Bearer <access_token>`.

The Notification Service is **purely reactive** - it never initiates actions. It only consumes RabbitMQ events and exposes HTTP endpoints for clients to read/mark notifications.

---

## REST Endpoints

### GET /notifications

Fetch notifications for the authenticated user.

**Query params:**
| Param | Type | Description |
|-------|------|-------------|
| `read` | bool | Filter by read status. `false` = unread only. Omit for all. |
| `limit` | int | Max results (default 50, max 100) |
| `before` | uuid | Cursor for pagination |

**Response `200`:**
```json
[
  {
    "id": "<uuid>",
    "user_id": "<uuid>",
    "type": "mention",
    "payload": {
      "channel_id": "<uuid>",
      "guild_id": "<uuid>",
      "author_id": "<uuid>",
      "preview": "T'es mauvais @yandry"
    },
    "read": false,
    "created_at": "2026-03-09T12:00:00Z"
  }
]
```

**Notification types and their payloads:**

| `type` | Payload fields |
|--------|---------------|
| `mention` | `channel_id`, `guild_id`, `author_id`, `preview` |
| `dm` | `sender_id`, `preview` |
| `friend_request` | `from_user_id` |
| `guild_invite` | `guild_id`, `guild_name`, `from_user_id` |
| `guild_welcome` | `guild_id`, `guild_name` |
| `incoming_call` | `call_type` (`"audio"` or `"video"`) |

---

### PATCH /notifications/{id}/read

Mark a single notification as read.

**Request body:** None

**Response `200`:**
```json
{
  "id": "<uuid>",
  "read": true
}
```

**Errors:**
| Status | Reason |
|--------|--------|
| 403 | Notification does not belong to this user |
| 404 | Notification not found |

---

### PATCH /notifications/read-all

Mark all notifications as read for the authenticated user.

**Request body:** None

**Response `200`:**
```json
{
  "updated": 12
}
```

---

## SignalR - Real-time delivery

The Notification Service pushes real-time alerts via SignalR (either its own hub or shared with Chat Service - implementation detail). When a notification is created for a user who is currently connected, the service immediately pushes it:

**Server → Client event: `ReceiveNotification`**

```json
{
  "target": "ReceiveNotification",
  "arguments": [{
    "id": "<uuid>",
    "type": "mention",
    "payload": { ... },
    "created_at": "2026-03-09T12:00:00Z"
  }]
}
```

If the user is not connected, the notification row stays in DB and is fetched on next login via `GET /notifications?read=false`.

---

## RabbitMQ Events Consumed

| Event | Trigger | Notification created |
|-------|---------|---------------------|
| `chat.message_sent` | User is in `mentions[]` | `type: mention` for each mentioned user |
| `call.incoming` | Always | `type: incoming_call` for `callee_id` - prompts them to open the app |
| `friend.request_sent` | Always | `type: friend_request` for `to_user_id` |
| `guild.invite_created` | Always | `type: guild_invite` for `invited_user_id` |
| `guild.member_joined` | Always | `type: guild_welcome` for the joining user |

