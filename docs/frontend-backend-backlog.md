# Frontend / Backend Integration Backlog

Status legend:
- `[ ]` not started
- `[-]` in progress
- `[x]` done

This backlog is ordered by dependency, not by visual priority.

## Epic 0 - Contract alignment and shell plumbing

Goal: make the frontend speak the same protocol as the backend services before wiring feature UI.

- [ ] Replace fake auth/session flow with real token handling
  - [ ] `login` and `register` must call `/auth/login` and `/auth/register` with `email + password`
  - [ ] persist `access_token` from auth responses
  - [ ] replace `createFakeSession` with real session bootstrap and logout cleanup
  - [ ] add refresh-token recovery path on 401 for authenticated requests
- [ ] Normalize environment and gateway base URLs
  - [ ] verify the `NEXT_PUBLIC_API_*_URL` values for auth, user, guild, chat, notification
  - [ ] add a shared runtime health check page or dev banner when a service URL is missing
  - [ ] document the expected gateway paths for each service
- [ ] Define the canonical frontend DTO layer
  - [ ] map backend snake_case payloads to UI-friendly view models
  - [ ] create one transformation layer per service
  - [ ] stop letting components depend on raw API payloads directly

## Epic 1 - Authentication and account lifecycle

Goal: complete sign-in, sign-up, and account management flows.

- [ ] Login form
  - [ ] switch the form fields from `username` to `email`
  - [ ] surface auth errors with backend status mapping
  - [ ] redirect authenticated users away from `/auth/login` and `/auth/register`
- [ ] Registration form
  - [ ] align request body with backend contract
  - [ ] remove fake account creation logic
  - [ ] handle email duplication and weak password errors
- [ ] Account actions
  - [ ] add `GET /auth/me` consumption for the account menu
  - [ ] add logout via `POST /auth/logout`
  - [ ] add delete-account flow with the `409` guild ownership guard
  - [ ] add `PATCH /auth/me` for email/password changes
- [ ] OAuth entry points
  - [ ] add login buttons for `42`, `Google`, and `GitHub`
  - [ ] handle callback redirects and token handoff

## Epic 2 - User profile, friends, and social graph

Goal: replace all local friend/profile placeholders with real user-service data.

- [ ] Current user profile
  - [ ] fetch `GET /users/me` on app load
  - [ ] hydrate username, display name, avatar, banner, bio, and presence
  - [ ] use the profile in the chat sidebar and settings modal
- [ ] Profile editing
  - [ ] wire `PATCH /users/{id}` for display name, bio, and status
  - [ ] wire avatar upload and avatar removal
  - [ ] wire banner upload and banner removal
- [ ] Friend discovery
  - [ ] wire `GET /users/search?q=`
  - [ ] allow friend-add by username via `POST /friends`
  - [ ] handle acceptance, blocking, and deletion with friendship IDs
- [ ] Friend panels
  - [ ] replace `friend-mocks` with `GET /users/{id}/friends`
  - [ ] add pending requests tab with `GET /users/me/friend-requests`
  - [ ] add relationship state rendering with `GET /users/me/friendship/{user_id}`
  - [ ] add block/unblock actions with `/users/{id}/block`

## Epic 3 - Guild discovery and membership management

Goal: connect the guild sidebar and guild page to live guild data.

- [ ] Guild sidebar
  - [ ] fetch `GET /guilds/me` on login
  - [ ] sort guilds by `joined_at` and preserve selection
  - [ ] replace the random guild generator with live guild creation
- [ ] Guild landing page
  - [ ] show the selected guild details from `GET /guilds/{id}`
  - [ ] add create-guild form using `POST /guilds`
  - [ ] add join-guild via invite code using `POST /guilds/{id}/join`
- [ ] Guild membership list
  - [ ] fetch `GET /guilds/{id}/members`
  - [ ] hydrate members via `GET /users?ids=...`
  - [ ] display nicknames, avatars, roles, and join timestamps
- [ ] Guild admin actions
  - [ ] wire nickname edits with `PATCH /guilds/{id}/members/{user_id}`
  - [ ] wire member kick with `DELETE /guilds/{id}/members/{user_id}`
  - [ ] wire bans list, ban creation, and unban actions
  - [ ] wire invite creation, listing, preview, and revoke
- [ ] Guild settings
  - [ ] add edit guild settings with `PATCH /guilds/{id}`
  - [ ] add delete guild with owner-only confirmation
  - [ ] add roles and categories management UI on top of existing endpoints

## Epic 4 - Chat history, composer, and realtime transport

Goal: remove message mocks and make `/chat` use the real chat service.

- [ ] Channel history
  - [ ] fetch `GET /channels/{channel_id}/messages`
  - [ ] paginate upward with `before_time`
  - [ ] render attachments, reactions, edited state, and reply chains
- [ ] Direct messages
  - [ ] fetch `GET /dms`
  - [ ] fetch `GET /dms/{user_id}/messages`
  - [ ] support archived conversations via `DELETE /dms/{user_id}` and `include_archived=true`
- [ ] Composer
  - [ ] send guild messages with `POST /channels/{channel_id}/messages`
  - [ ] send DMs with `POST /dms/{user_id}/messages`
  - [ ] support optimistic bubbles using `nonce`
  - [ ] support editing and deleting messages
- [ ] Attachments
  - [ ] upload files with `POST /attachments`
  - [ ] attach draft uploads to outgoing messages
  - [ ] render attachment previews and downloads
- [ ] Reactions and read state
  - [ ] add reaction toggles for channel messages
  - [ ] mark channel read state with `PUT /channels/{id}/read`
  - [ ] mark DM read state with `PUT /dms/{user_id}/read`
  - [ ] surface unread counts in the channel and DM sidebars
- [ ] SignalR
  - [ ] connect to `/hubs/chat`
  - [ ] handle `ReceiveMessage`, `ReceiveDirectMessage`, `MessageEdited`, `MessageDeleted`
  - [ ] handle presence and read-state events
  - [ ] reconnect cleanly after transport loss

## Epic 5 - Notifications

Goal: replace the notification drawer mock with the reactive notification service.

- [ ] Notification feed
  - [ ] fetch `GET /notifications`
  - [ ] support `read`, `include_dismissed`, and cursor pagination
  - [ ] render notification types with service-specific payloads
- [ ] Notification actions
  - [ ] mark individual notifications with `PATCH /notifications/{id}/read`
  - [ ] mark all notifications with `PATCH /notifications/read-all`
  - [ ] dismiss notifications with `DELETE /notifications/{id}`
- [ ] Badge counter
  - [ ] fetch `GET /notifications/unread-count`
  - [ ] keep the badge in sync with real-time updates
- [ ] Live updates
  - [ ] connect notification SignalR delivery
  - [ ] reconcile server pushes with local cache and pagination
  - [ ] suppress notifications according to mute preferences
- [ ] Preferences
  - [ ] wire `GET /notifications/preferences`
  - [ ] add mute/unmute by guild and channel scope

## Epic 6 - Cross-service consistency and UX resilience

Goal: keep the app coherent when services disagree, disconnect, or return partial data.

- [ ] Error model
  - [ ] standardize error parsing across all service clients
  - [ ] map 401 / 403 / 404 / 409 into explicit UI states
  - [ ] add retry affordances for transient failures
- [ ] Loading states
  - [ ] add skeletons or placeholders for guilds, DMs, members, notifications, and history
  - [ ] prevent stale mock data from flashing before the first fetch completes
- [ ] Cache and invalidation
  - [ ] centralize optimistic updates for message send, friend request, and notification read state
  - [ ] invalidate dependent views after mutations
  - [ ] preserve scroll and selection state when data refreshes
- [ ] Offline and disconnect behavior
  - [ ] define what the frontend shows when a service is unreachable
  - [ ] make session expiration recoverable without a full page crash
  - [ ] document expected fallback behavior for chat and notifications

## Suggested delivery order

1. Epic 0: contract alignment and session plumbing
2. Epic 1: auth lifecycle
3. Epic 2: user and friends
4. Epic 3: guild discovery and membership
5. Epic 4: chat transport and history
6. Epic 5: notifications
7. Epic 6: resilience and cache consistency

