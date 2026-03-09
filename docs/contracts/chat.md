# API Contract - Chat Service

The Chat Service has two interfaces:
1. **REST** - message history, edit/delete
2. **SignalR WebSocket hub** - real-time send/receive, presence

Base path: `/chat` (REST), `/hubs/chat` (SignalR)

All REST responses are `application/json`. Errors follow the shape:
```json
{ "error": "human-readable message" }
```

All endpoints require `Authorization: Bearer <access_token>`.

---

## REST Endpoints

### GET /channels/{channel_id}/messages

Fetch paginated message history for a channel.

**Query params:**
| Param | Type | Description |
|-------|------|-------------|
| `before` | uuid | Cursor - return messages before this message ID |
| `limit` | int | Number of messages to return (default 50, max 100) |

**Response `200`:**
```json
[
  {
    "id": "<uuid>",
    "channel_id": "<uuid>",
    "author_id": "<uuid>",
    "content": "P'tit chien enragé?",
    "edited_at": null,
    "created_at": "2026-03-09T12:00:00Z"
  }
]
```

Messages are returned newest-first.

**Errors:**
| Status | Reason |
|--------|--------|
| 403 | User is not a member / lacks `READ_MESSAGES` permission |
| 404 | Channel not found |

---

### PATCH /messages/{id}

Edit a message. Only the author can edit their own messages.

**Request body:**
```json
{
  "content": "Updated content"
}
```

**Response `200`:**
```json
{
  "id": "<uuid>",
  "content": "P'tit coca rigolo?",
  "edited_at": "2026-03-09T12:05:00Z"
}
```

**Errors:**
| Status | Reason |
|--------|--------|
| 403 | Not the author |
| 404 | Message not found |

**Side effects:** Broadcasts `MessageEdited` event to all clients in the SignalR channel group.

---

### DELETE /messages/{id}

Delete a message. Author or member with `MANAGE_MESSAGES` permission.

**Response `204`:** No content.

**Errors:**
| Status | Reason |
|--------|--------|
| 403 | Not the author and lacks `MANAGE_MESSAGES` |
| 404 | Message not found |

**Side effects:** Broadcasts `MessageDeleted { message_id, channel_id }` to the SignalR channel group.

---

### GET /dms

List all DM conversations for the authenticated user.

**Response `200`:**
```json
[
  {
    "partner_id": "<uuid>",
    "last_message": {
      "content": "mec ta partie elle leak de fou",
      "created_at": "2026-03-09T11:00:00Z"
    },
    "unread_count": 3
  }
]
```

---

### GET /dms/{user_id}/messages

Fetch DM message history with a specific user.

**Query params:**
| Param | Type | Description |
|-------|------|-------------|
| `before` | uuid | Cursor |
| `limit` | int | Default 50, max 100 |

**Response `200`:**
```json
[
  {
    "id": "<uuid>",
    "sender_id": "<uuid>",
    "recipient_id": "<uuid>",
    "content": "faut normer ta partie d'ailleurs :wittle_guy:",
    "created_at": "2026-03-09T11:00:00Z"
  }
]
```

**Errors:**
| Status | Reason |
|--------|--------|
| 404 | No DM conversation found |

---

## SignalR Hub - `/hubs/chat`

Connect with the access token:
```
wss://<host>/hubs/chat?access_token=<jwt>
```

On connect, the server adds the client to all SignalR groups corresponding to the channels they are a member of, plus a personal group for DMs.

---

### Client → Server (Invocations)

#### SendMessage

Send a message to a channel.

```json
{
  "target": "SendMessage",
  "arguments": [{
    "channel_id": "<uuid>",
    "content": "toi multithreading"
  }]
}
```

Server actions:
1. HTTP GET to Guild Service `/channels/{channel_id}/membership?user_id=...` - validates membership + `SEND_MESSAGES` permission
2. Persists message in DB
3. Broadcasts `ReceiveMessage` to the channel group
4. Publishes `chat.message_sent` to RabbitMQ

**Error:** Hub sends `Error` event back to the caller if validation fails.

---

#### SendDirectMessage

Send a DM to another user.

```json
{
  "target": "SendDirectMessage",
  "arguments": [{
    "recipient_id": "<uuid>",
    "content": "devine qui c'est qui tourne :)"
  }]
}
```

---

### Server → Client (Events)

#### ReceiveMessage

Broadcast to all members of a channel when a new message arrives.

```json
{
  "target": "ReceiveMessage",
  "arguments": [{
    "id": "<uuid>",
    "channel_id": "<uuid>",
    "author_id": "<uuid>",
    "content": "viens on fait minishell",
    "created_at": "2026-03-09T12:00:00Z"
  }]
}
```

---

#### ReceiveDirectMessage

Sent to the recipient's personal group when a DM arrives.

```json
{
  "target": "ReceiveDirectMessage",
  "arguments": [{
    "id": "<uuid>",
    "sender_id": "<uuid>",
    "content": "y a des merge conflicts d'ailleurs",
    "created_at": "2026-03-09T12:00:00Z"
  }]
}
```

---

#### MessageEdited

```json
{
  "target": "MessageEdited",
  "arguments": [{
    "id": "<uuid>",
    "channel_id": "<uuid>",
    "content": "et la p'tite update du message là mhmmm",
    "edited_at": "2026-03-09T12:05:00Z"
  }]
}
```

---

#### MessageDeleted

```json
{
  "target": "MessageDeleted",
  "arguments": [{
    "message_id": "<uuid>",
    "channel_id": "<uuid>"
  }]
}
```

---

#### UserPresence

Broadcast to all shared guild members when a user connects or disconnects.

```json
{
  "target": "UserPresence",
  "arguments": [{
    "user_id": "<uuid>",
    "status": "online"
  }]
}
```

`status` is `online` or `offline`.

---

## RabbitMQ Events Published

| Event | Payload | Trigger |
|-------|---------|---------|
| `chat.message_sent` | `{ channel_id, guild_id, author_id, content, mentions: [uuid] }` | Message sent in a channel |
| `user.online` | `{ user_id }` | Client connects to hub |
| `user.offline` | `{ user_id }` | Client disconnects from hub |

## RabbitMQ Events Consumed

| Event | Action |
|-------|--------|
| `guild.member_joined` | Add user to SignalR groups for all channels in that guild |
| `guild.member_left` | Remove user from SignalR groups for that guild |
| `user.logged_out` | Force-disconnect any active WebSocket for that user |
