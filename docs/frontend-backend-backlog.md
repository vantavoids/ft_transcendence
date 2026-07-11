# Frontend / Backend Integration Backlog

Status legend:
- `[ ]` not started
- `[-]` in progress
- `[x]` done

This backlog is ordered by dependency, not by visual priority.

## Epic 0 - Contract alignment and shell plumbing

Goal: make the frontend speak the same protocol as the backend services before wiring feature UI.

- [x] Replace fake auth/session flow with real token handling
  - [x] `login` and `register` must call `/auth/login` and `/auth/register` with `email + password`
  - [x] persist `access_token` from auth responses
  - [x] replace `createFakeSession` with real session bootstrap and logout cleanup
  - [x] add refresh-token recovery path on 401 for authenticated requests
- [-] Normalize environment and gateway base URLs
  - [x] verify the `NEXT_PUBLIC_API_*_URL` values for auth, user, guild, chat, notification
        (collapsed to a single `NEXT_PUBLIC_API_URL`; the gateway routes by `/{service}/v1` under it)
  - [ ] add a shared runtime health check page or dev banner when a service URL is missing
  - [x] document the expected gateway paths for each service (see `.env.example` and `src/shared/api/client.ts`)
- [-] Define the canonical frontend DTO layer
  - [-] map backend snake_case payloads to UI-friendly view models
        (`src/shared/mappers/chat.ts` only - auth/user/guild/notification have no mapper yet)
  - [ ] create one transformation layer per service
  - [ ] stop letting components depend on raw API payloads directly

## Epic 1 - Authentication and account lifecycle

Goal: complete sign-in, sign-up, and account management flows.

- [x] Login form
  - [x] switch the form fields from `username` to `email`
  - [x] surface auth errors with backend status mapping
  - [x] redirect authenticated users away from `/auth/login` and `/auth/register` (middleware `proxy.ts`)
- [x] Registration form
  - [x] align request body with backend contract
  - [x] remove fake account creation logic
  - [x] handle email duplication and weak password errors
- [x] Account actions
  - [x] add `GET /auth/me` consumption for the account menu (settings modal)
  - [x] add logout via `POST /auth/logout`
  - [x] add delete-account flow with the `409` guild ownership guard
  - [x] add `PATCH /auth/me` for email/password changes
- [x] OAuth entry points
  - [x] add login buttons for `42`, `Google`, and `GitHub` (slugs `fortytwo`, `google`, `github`)
  - [x] handle callback redirects and token handoff (`/?access_token=...` -> `OAuthHandoff`)

## Epic 2 - User profile, friends, and social graph

Goal: replace all local friend/profile placeholders with real user-service data.

- [x] Current user profile
  - [x] fetch `GET /users/me` on app load
  - [x] hydrate username, display name, avatar, banner, bio, and presence
  - [x] use the profile in the chat sidebar and settings modal
- [x] Profile editing
  - [x] wire `PATCH /users/{id}` for display name, bio, and status
  - [x] wire avatar upload and avatar removal
  - [x] wire banner upload and banner removal
  - [x] add shared profile validation rules for display name, bio, avatar, and banner limits
- [x] Friend discovery
  - [x] wire `GET /users/search?q=`
  - [x] allow friend-add by username via `POST /friends`
  - [x] handle acceptance, blocking, and deletion with friendship IDs
- [x] Friend panels
  - [x] replace `friend-mocks` with `GET /users/{id}/friends`
  - [x] add pending requests tab with `GET /users/me/friend-requests`
  - [x] add relationship state rendering with `GET /users/me/friendship/{user_id}`
  - [x] add block/unblock actions with `/users/{id}/block`

## Epic 3 - Guild discovery and membership management

Goal: connect the guild sidebar and guild page to live guild data.

- [x] Guild sidebar
  - [x] fetch `GET /guilds/me` on login
  - [x] sort guilds by `joined_at` and preserve selection
  - [x] replace the random guild generator with live guild creation
- [x] Guild landing page
  - [x] show the selected guild details from `GET /guilds/{id}`
  - [x] add create-guild form using `POST /guilds`
  - [x] add join-guild via invite code using `POST /guilds/{id}/join`
- [x] Guild membership list
  - [x] fetch `GET /guilds/{id}/members`
  - [x] hydrate members via `GET /users?ids=...`
  - [x] display nicknames, avatars, roles, and join timestamps
- [x] Guild admin actions
  - [x] wire nickname edits with `PATCH /guilds/{id}/members/{user_id}`
  - [x] wire member kick with `DELETE /guilds/{id}/members/{user_id}`
  - [x] wire bans list, ban creation, and unban actions
  - [x] wire invite creation, listing, preview, and revoke
- [x] Guild settings
  - [x] add edit guild settings with `PATCH /guilds/{id}`
  - [x] add delete guild with owner-only confirmation
  - [x] add roles and categories management UI on top of existing endpoints
- [x] Channel management
  - [x] wire channel creation with `POST /guilds/{id}/channels`
  - [x] wire channel rename/topic/nsfw/slowmode edits with `PATCH /guilds/{id}/channels/{channel_id}`
  - [x] wire channel delete with `DELETE /guilds/{id}/channels/{channel_id}`

## Epic 4 - Chat history, composer, and realtime transport

Goal: remove message mocks and make `/chat` use the real chat service.

- [x] Channel history
  - [x] fetch `GET /channels/{channel_id}/messages`
  - [x] paginate upward with `before_time`
  - [x] render attachments, reactions, edited state, and reply chains
  - [x] hydrate message authors and profile cards via `GET /users?ids=...`
- [x] Direct messages
  - [x] fetch `GET /dms`
  - [x] fetch `GET /dms/{user_id}/messages`
  - [x] support archived conversations via `DELETE /dms/{user_id}` and `include_archived=true`
- [x] Composer
  - [x] send guild messages with `POST /channels/{channel_id}/messages`
  - [x] send DMs with `POST /dms/{user_id}/messages`
  - [x] support optimistic bubbles using `nonce`
  - [x] support editing and deleting messages
- [x] Attachments
  - [x] upload files with `POST /attachments`
  - [x] attach draft uploads to outgoing messages
  - [x] render attachment previews and downloads
- [x] Reactions and read state
  - [x] add reaction toggles for channel messages
  - [x] mark channel read state with `PUT /channels/{id}/read`
  - [x] mark DM read state with `PUT /dms/{user_id}/read`
  - [x] surface unread counts in the channel and DM sidebars
- [x] SignalR
  - [x] connect to `/hubs/chat`
  - [x] handle `ReceiveMessage`, `ReceiveDirectMessage`, `MessageEdited`, `MessageDeleted`
  - [x] handle presence and read-state events
  - [x] reconnect cleanly after transport loss

## Epic 5 - Notifications

Goal: replace the notification drawer mock with the reactive notification service.

- [x] Notification feed
  - [x] fetch `GET /notifications`
  - [x] support `read`, `include_dismissed`, and cursor pagination
  - [x] render notification types with service-specific payloads
- [x] Notification actions
  - [x] mark individual notifications with `PATCH /notifications/{id}/read`
  - [x] mark all notifications with `PATCH /notifications/read-all`
  - [x] dismiss notifications with `DELETE /notifications/{id}`
- [x] Badge counter
  - [x] fetch `GET /notifications/unread-count`
  - [x] keep the badge in sync with real-time updates
- [x] Live updates
  - [x] connect notification SignalR delivery
        (the notification contract ships SSE, not SignalR: `EventSource` on
        `GET /notifications/events` with the named `ReceiveNotification` event)
  - [x] reconcile server pushes with local cache and pagination
  - [x] suppress notifications according to mute preferences
- [x] Preferences
  - [x] wire `GET /notifications/preferences`
  - [x] add mute/unmute by guild and channel scope

## Epic 6 - Cross-service consistency and UX resilience

Goal: keep the app coherent when services disagree, disconnect, or return partial data.

- [-] Error model
  - [-] standardize error parsing across all service clients
        (`ApiError` in `src/shared/api/client.ts` centralizes status/body; per-status mapping only exists
        for auth via `src/shared/lib/auth-errors.ts` - user/guild/notification clients are unused so far)
  - [ ] map 401 / 403 / 404 / 409 into explicit UI states
  - [ ] add retry affordances for transient failures
- [ ] Loading states
  - [ ] add skeletons or placeholders for guilds, DMs, members, notifications, and history
  - [ ] prevent stale mock data from flashing before the first fetch completes
- [-] Cache and invalidation
  - [-] centralize optimistic updates for message send, friend request, and notification read state
        (done for chat only - `use-conversation-history.ts`; friend request/notification read state not wired yet)
  - [ ] invalidate dependent views after mutations
  - [x] preserve scroll and selection state when data refreshes (`use-scroll-preservation.ts`, chat only)
- [-] Offline and disconnect behavior
  - [ ] define what the frontend shows when a service is unreachable
  - [x] make session expiration recoverable without a full page crash
        (single-flight 401 refresh in `src/shared/api/client.ts` covers every service client)
  - [-] document expected fallback behavior for chat and notifications
        (chat hub auto-reconnects and rejoins channels via `use-guild-workspace.ts`; nothing documented, notifications N/A yet)

## Suggested delivery order

1. Epic 0: contract alignment and session plumbing
2. Epic 1: auth lifecycle
3. Epic 2: user and friends
4. Epic 3: guild discovery and membership
5. Epic 4: chat transport and history
6. Epic 5: notifications
7. Epic 6: resilience and cache consistency
