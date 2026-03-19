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

> **Storage note:** Chat Service uses **ScyllaDB** (Cassandra-compatible wide-column store).
> - Channel messages are partitioned by `channel_id`, clustered by `(created_at DESC, id)`.
> - DM messages are partitioned by `conversation_id` (deterministic UUID derived from the two user IDs), clustered by `(created_at DESC, id)`.
> - **Pagination is cursor-based using `before_time` (ISO 8601 timestamp) - not offset.** ScyllaDB has no concept of row offsets; all range scans use clustering key bounds.
> - Edit and delete by message `id` alone require an internal auxiliary lookup (`message_id → channel_id + created_at`). This is transparent to API callers.

---

## REST Endpoints

### GET /channels/{channel_id}/messages

Fetch paginated message history for a channel.

**Query params:**
| Param | Type | Description |
|-------|------|-------------|
| `before_time` | ISO 8601 timestamp | Cursor - return messages with `created_at < before_time`. Use the `created_at` of the oldest message in the previous page. Omit for the first page (returns the most recent messages). |
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

Messages are returned newest-first. To load more (scroll up), pass `before_time` = the `created_at` of the last (oldest) message in the current page.

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
| `before_time` | ISO 8601 timestamp | Cursor - return messages with `created_at < before_time`. Omit for the first page. |
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

On connect, the server:
1. Adds the client to all SignalR groups for their guild channels, plus a personal group for DMs.
2. Checks for a pending incoming call (caller sent `CallOffer` while this user was offline) - if one exists, immediately relays `IncomingCall` to the client so they can answer.

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

---

## WebRTC Signaling (via SignalR Hub)

Voice and video calls are **1-on-1, peer-to-peer via WebRTC**. The Chat Service hub acts as the signaling server - it relays SDP offers/answers and ICE candidates between the two peers. No media ever touches the server.

### Call flow

```
Caller                      Chat Hub                     Callee
  │                             │                            │
  ├─ CallOffer ────────────────►│                            │
  │  { callee_id, call_id,      │─── IncomingCall ──────────►│
  │    sdp, call_type }         │    { caller_id, call_id,   │
  │                             │      sdp, call_type }      │
  │                             │                            │
  │                             │◄── CallAnswer ─────────────┤
  │◄─── CallAnswered ───────────┤    { call_id, sdp }        │
  │     { call_id, sdp }        │                            │
  │                             │                            │
  ├─ IceCandidate ─────────────►│─── IceCandidate ──────────►│
  │◄─── IceCandidate ───────────┤◄── IceCandidate ───────────┤
  │     (relay, both ways)      │    (relay, both ways)      │
  │                             │                            │
  │  [P2P connection established - media flows directly]     │
  │                             │                            │
  ├─ CallHangup ───────────────►│─── CallHungUp ────────────►│
     { call_id }                │    { call_id }
```

If the callee **rejects** the call:
```
Callee ── CallReject ──► Hub ── CallRejected ──► Caller
```

If the callee is **offline or busy**, the Hub immediately replies to the caller with `CallFailed`.

If neither party hangs up within **30 seconds of `IncomingCall`** with no answer, the Hub sends `CallFailed { reason: "timeout" }` to the caller and `CallMissed { call_id, caller_id }` to the callee.

---

### Client → Server (Invocations) - Signaling

#### CallOffer

Initiate a call. Caller generates `call_id` (UUID).

```json
{
  "target": "CallOffer",
  "arguments": [{
    "callee_id": "<uuid>",
    "call_id": "<uuid>",
    "call_type": "video",
    "sdp": "<SDP offer string>"
  }]
}
```

`call_type`: `"audio"` or `"video"`.

Server actions:
1. Check callee is not already in a call → if busy, send `CallFailed { call_id, reason: "user_busy" }` to caller
2. Check callee is connected to hub
   - **Connected** → relay `IncomingCall` to callee's personal group immediately
   - **Not connected** → publish `call.incoming` to RabbitMQ `{ call_id, caller_id, callee_id, call_type }`. Notification Service will notify the callee. `IncomingCall` will be relayed when the callee next connects to the hub (see OnConnect above).
3. Store the pending call state server-side (caller, callee, sdp, call_type)
4. Start a 2-minute answer timeout - on expiry, send `CallFailed { call_id, reason: "timeout" }` to caller and clear call state

---

#### CallAnswer

Accept an incoming call. Callee responds with their SDP answer.

```json
{
  "target": "CallAnswer",
  "arguments": [{
    "call_id": "<uuid>",
    "sdp": "<SDP answer string>"
  }]
}
```

Server relays `CallAnswered` to the caller's personal group.

---

#### CallReject

Decline an incoming call.

```json
{
  "target": "CallReject",
  "arguments": [{
    "call_id": "<uuid>"
  }]
}
```

Server relays `CallRejected` to the caller's personal group.

---

#### CallHangup

End an ongoing or ringing call (usable by either party at any point after `CallOffer`).

```json
{
  "target": "CallHangup",
  "arguments": [{
    "call_id": "<uuid>"
  }]
}
```

Server relays `CallHungUp` to the other party's personal group and clears call state.

---

#### IceCandidate

Relay a trickle ICE candidate to the remote peer.

```json
{
  "target": "IceCandidate",
  "arguments": [{
    "call_id": "<uuid>",
    "candidate": "<ICE candidate string>",
    "sdp_mid": "<string>",
    "sdp_mline_index": 0
  }]
}
```

Server relays `IceCandidate` verbatim to the other party's personal group.

---

### Server → Client (Events) - Signaling

#### IncomingCall

Sent to the callee's personal group when someone calls them.

```json
{
  "target": "IncomingCall",
  "arguments": [{
    "call_id": "<uuid>",
    "caller_id": "<uuid>",
    "call_type": "video",
    "sdp": "<SDP offer string>"
  }]
}
```

---

#### CallAnswered

Sent to the caller when the callee accepts.

```json
{
  "target": "CallAnswered",
  "arguments": [{
    "call_id": "<uuid>",
    "sdp": "<SDP answer string>"
  }]
}
```

---

#### CallRejected

Sent to the caller when the callee declines.

```json
{
  "target": "CallRejected",
  "arguments": [{
    "call_id": "<uuid>"
  }]
}
```

---

#### CallHungUp

Sent to the other party when someone ends the call.

```json
{
  "target": "CallHungUp",
  "arguments": [{
    "call_id": "<uuid>"
  }]
}
```

---

#### CallFailed

Sent to the caller when the call cannot be established (callee offline, busy, or timeout).

```json
{
  "target": "CallFailed",
  "arguments": [{
    "call_id": "<uuid>",
    "reason": "user_offline"
  }]
}
```

`reason`: `"user_offline"` | `"user_busy"` | `"timeout"`

---

#### IceCandidate

Relayed verbatim from the remote peer.

```json
{
  "target": "IceCandidate",
  "arguments": [{
    "call_id": "<uuid>",
    "candidate": "<ICE candidate string>",
    "sdp_mid": "<string>",
    "sdp_mline_index": 0
  }]
}
```

---

## RabbitMQ Events Published

| Event | Payload | Trigger |
|-------|---------|---------|
| `chat.message_sent` | `{ channel_id, guild_id, author_id, content, mentions: [uuid] }` | Message sent in a channel |
| `call.incoming` | `{ call_id, caller_id, callee_id, call_type }` | Callee was offline when `CallOffer` arrived - Notification Service notifies them to open the app |
| `user.online` | `{ user_id }` | Client connects to hub |
| `user.offline` | `{ user_id }` | Client disconnects from hub |

## RabbitMQ Events Consumed

| Event | Action |
|-------|--------|
| `guild.member_joined` | Add user to SignalR groups for all channels in that guild |
| `guild.member_left` | Remove user from SignalR groups for that guild |
| `user.logged_out` | Force-disconnect any active WebSocket for that user |
