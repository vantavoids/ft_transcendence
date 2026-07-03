*This project has been created as part of the 42 curriculum by yandry, tstephan, jramiro, achu, sliziard.*

---

# ft_discord

A real-time collaborative messaging platform built as the final project of the 42 Common Core. Inspired by Discord, but much better \<3, it features servers (guilds), channels, direct messages, real-time chat, a friend system, a full role/permission system and of course voice / video calls! All backed by an event-driven microservices architecture because we love to suffer :)

---

## Table of Contents

- [Description](#description)
- [Instructions](#instructions)
- [Resources](#resources)
- [Team Information](#team-information)
- [Project Management](#project-management)
- [Technical Stack](#technical-stack)
- [Database Schema](#database-schema)
- [Features List](#features-list)
- [Modules](#modules)
- [Individual Contributions](#individual-contributions)

---

## Description

**ft_discord** is a full-stack web application that replicates the core experience of a real-time messaging platform. Users can register, create and join servers (guilds), communicate in text channels, send direct messages, manage friends, call their friends on voice or video and interact with a rich notification system.

### Key Features

- Real-time messaging via WebSockets (SignalR)
- Server (guild) creation with channels, categories and role-based permissions
- Friend system with online presence indicators
- Direct messaging and a real-time notification system
- 1-on-1 voice and video calls via WebRTC
- Email + password authentication and OAuth 2.0 (Google, GitHub, 42)
- Event-driven backend with a message queue (RabbitMQ)
- Polyglot microservices architecture, each service independently deployable
- Monitoring stack: Prometheus + Grafana dashboards fed by per-service OpenTelemetry exporters
- Containerized and launchable with a single command

---

## Instructions

### Prerequisites

- [Docker](https://docs.docker.com/get-docker/) and [Docker Compose](https://docs.docker.com/compose/), or [Podman](https://podman.io/) + [`podman-compose`](https://github.com/containers/podman-compose) (the `make` flow autodetects the engine, preferring `podman` / `podman-compose` and falling back to `docker` / `docker compose`; force one with `ENGINE=podman make` or, if you're normal and prefer Docker, `ENGINE=docker make`)
- [GNU Make](https://www.gnu.org/software/make/) (the entry point for every command)
- [Go](https://go.dev/dl/) (only needed once for `tools/secretman` bootstrap)
- [Tilt](https://tilt.dev) (only needed for the local development loop, not for the eval `make` flow)
- An `age` key registered in `tools/secretman/.sops.yaml` (one of the maintainers can add yours)

### Setup

The full setup is driven by `Makefile` targets at the repo root. A normal first-run looks like:

```bash
# 1. Clone the repository
git clone git@github.com:vantavoids/ft_transcendence.git ft_discord
cd ft_discord

# 2. Bootstrap secretman (downloads SOPS + age binaries on first run, registers your age key)
make secrets-setup

# 3. Decrypt every per-service .env file in place
make secrets-decrypt

# 4. Build and start the full stack (runs check-env, then build, then up -d)
make
```

The application will be available at [**https://localhost:1443**](https://localhost:1443). NGINX terminates TLS on `1443`; `1080` redirects HTTP to HTTPS.

### Make Targets

| Target | Description |
| --- | --- |
| `make` (alias for `make all`) | `check-env`  • `build`  • `up -d`  • the canonical "bring the project up" command |
| `make build` | Build all images |
| `make up` | Start the stack in detached mode |
| `make down` | Stop the stack |
| `make re` | `fclean` + `all` (scorched-earth rebuild) |
| `make clean` | `down` + prune dangling images |
| `make fclean` | Nuke containers, images and volumes (use with care) |
| `make rmcert` | Remove the generated `mkcert` certificates |
| `make logs` | Tail every container's logs (`Ctrl+C` to detach) |
| `make ps` | List running containers |
| `make login` | `docker login` / `podman login` against docker.io for image pulls |
| `make dev` | Bring the stack up under Tilt with file-watching live reload |
| `make secrets-setup` | Bootstrap `tools/secretman` and register your age key |
| `make secrets-decrypt` | Decrypt every per-service `.env.*` file in place |
| `make secrets-encrypt` | Re-encrypt every per-service `.env.*` file |
| `make secrets-refresh` | Re-encrypt with the current `.sops.yaml` recipient list (useful after a new contributor's key is added) |
| `make check-env` | Verify all expected `.env` files exist and have at least one populated variable |

### Environment Variables

`make check-env` runs before any build / start target and verifies that every `.env` file exists and has at least one populated variable. The expected files are:

```text
.env
frontend/.env
services/auth/.env
services/chat/.env
services/gateway/.env
services/guild/.env
services/notification/.env
services/user/.env
```

The root `.env` only carries values shared across every service (copy from `.env.example`):

| Variable | Description |
| --- | --- |
| `BASE_URL` | Public root URL of the stack (e.g. `https://localhost:1443`) |
| `BASE_API_URL` | Public API root (e.g. `https://localhost:1443/api`) |
| `JWT_SECRET` | Shared HMAC key used by Auth (sign) and the Gateway (verify) |
| `RABBITMQ_USER` | RabbitMQ default user |
| `RABBITMQ_PASS` | RabbitMQ default password |

OAuth client IDs / secrets, database passwords, the Grafana admin password, TURN credentials and other per-service secrets live in each service's own SOPS-encrypted env file under `services/<name>/` and `frontend/`, and are decrypted in place by `make secrets-decrypt`.

### Stopping the Application

```bash
make down
```

### Running in Development Mode

`make dev` brings the stack up under [Tilt](https://tilt.dev), so each service hot-reloads on file change via its own `Tiltfile`. Useful when you are iterating on a single service and do not want full `docker compose up --build` cycles.

---

## Resources

### Documentation

- [Next.js documentation](https://nextjs.org/docs)
- [React documentation](https://react.dev)
- [Tailwind CSS documentation](https://tailwindcss.com/docs)
- [Zod documentation](https://zod.dev)
- [ASP.NET Core documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [SignalR documentation](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction)
- [Entity Framework Core documentation](https://learn.microsoft.com/en-us/ef/core/)
- [Carter (HTTP modules for ASP.NET)](https://github.com/CarterCommunity/Carter)
- [MassTransit documentation](https://masstransit.io/documentation)
- [axum documentation](https://docs.rs/axum/)
- [sqlx documentation](https://docs.rs/sqlx/)
- [RabbitMQ documentation](https://www.rabbitmq.com/documentation.html)
- [ScyllaDB documentation](https://docs.scylladb.com/)
- [OpenTelemetry documentation](https://opentelemetry.io/docs/)
- [Prometheus documentation](https://prometheus.io/docs/)
- [Grafana documentation](https://grafana.com/docs/)
- [WebRTC API documentation](https://developer.mozilla.org/en-US/docs/Web/API/WebRTC_API)
- [coturn STUN/TURN server](https://github.com/coturn/coturn)
- [OAuth 2.0 RFC 6749](https://datatracker.ietf.org/doc/html/rfc6749)
- [Tilt documentation](https://docs.tilt.dev/)
- [SOPS documentation](https://github.com/getsops/sops)
- [WCAG 2.1 quick reference](https://www.w3.org/WAI/WCAG21/quickref/)

### AI Usage

to be defined

---

## Team Information

| Login | Role(s) | Responsibilities |
| --- | --- | --- |
| `yandry` | Product Owner, Developer | Product backlog, Guild Service (incl. roles & permissions), Chat Service (primary, real-time messaging via SignalR + ScyllaDB + WebRTC signaling), infrastructure & devops (Docker Compose, Tilt, NGINX, RabbitMQ, Prometheus/Grafana monitoring) |
| `tstephan` | Tech Lead, Developer | Technical architecture decisions, User Service (profiles, avatars, friends, presence), Frontend (shared with jramiro) |
| `sliziard` | Project Manager, Developer | Sprint planning, Auth Service (register/login/OAuth/JWT/refresh), Chat Service (co-developer) |
| `jramiro` | Developer | API Gateway, secret management tooling (`tools/secretman`), Frontend (shared with tstephan), monitoring (Prometheus/Grafana, shared with yandry) |
| `achu` | Developer, UX | Notification Service, Chat Service (co-developer), Privacy Policy & ToS pages |

---

## Service Ownership

Each backend service has a single designated owner who oversees its design, reviews contributions to it and is the person evaluators should question first about that service. Other teammates are welcome to send PRs against any service, but the owner holds the architectural line.

This mirrors how real companies running microservices actually operate. At places like Amazon, Spotify, Netflix or any modern shop where a product is split into independently-deployable services, each service is owned by a small team (Amazon famously caps them at "two pizzas") that is responsible end-to-end for its design, code, deployment, monitoring and on-call. Outside contributors can send PRs into another team's service, but the owning team is the architectural authority and the first reviewer; the broader org trusts the owning team to keep that service coherent rather than designing it by committee. We are scaled down to one person per service instead of a team per service, but the pattern is the same: one place to make architectural calls, one person whose name is on it during evaluation, no diffuse responsibility. This keeps decisions coherent across a polyglot codebase without freezing anyone out of any area, and it matches the way the team would scale if the project kept growing past five people.

| Service | Owner |
| --- | --- |
| Auth Service | `sliziard` |
| API Gateway | `jramiro` |
| User Service | `tstephan` |
| Guild Service | `yandry` |
| Notification Service | `achu` |
| Chat Service | `yandry` (designated; see note) |

**Chat is the exception.** It has been a collective effort with `sliziard` and `achu` contributing actively alongside `yandry`, rather than fitting cleanly under a single owner. We still designate `yandry` as the owner of record so that there is one person to ask about Chat-Service architecture at evaluation time, but credit on Chat is shared across all three.

---

## Project Management

### Task Distribution

Work was broken down into services aligned with team ownership. Each person owned one or more backend services end-to-end (design to implementation to tests to containerization to CI integration).

### Tools

- **GitHub Projects** ("The Holy Board"): backlog grooming, sprint planning, issue prioritization
- **GitHub Issues**: every feature, bug and chore tracked as an issue and pulled into a sprint iteration
- **GitHub Pull Requests**: every change reviewed by at least one other team member before merge, with linked issues
- **Discord**: daily async communication and weekly sync meetings

### Meeting Cadence

- Weekly sync: progress review, blockers, next sprint planning
- Async standups on Discord: daily updates in a dedicated channel

---

## Technical Stack

### Frontend

| Technology | Purpose |
| --- | --- |
| Next.js 16 (App Router) | React framework, routing, server components |
| React 19 + TypeScript 5.7 | UI components and type safety |
| Tailwind CSS 3.4 | Styling and design system |
| Zod | Runtime input validation (mirrors backend validation) |
| lucide-react | Icon set |
| pnpm | Package manager |
| ESLint + Prettier + Husky + lint-staged | Linting / formatting, enforced via pre-commit hook |

### Backend

The polyglot architecture lets each service pick the best tool for the job. Services communicate only via REST (sync, when an answer is required before continuing) and RabbitMQ events (async, fire-and-forget).

| Service | Language / Framework | Purpose |
| --- | --- | --- |
| API Gateway | Go 1.25 (`golang-jwt`) | Reverse proxy for `/api/{service}/vN/...`, local JWT validation, rate limiting |
| Auth Service | ASP.NET Core 10 (C#), Clean Architecture | Registration, login, OAuth 2.0 (Google / GitHub / 42), JWT issuance & refresh, account deletion |
| User Service | Rust (`axum` + `sqlx` + `tokio`) | Profiles, avatars, banners, friends, blocks, online status |
| Guild Service | ASP.NET Core 10 (C#), Clean Architecture, EF Core | Guilds, channels, categories, membership, roles, invites, bans, permission overwrites |
| Chat Service | ASP.NET Core 10 (C#), Clean Architecture, SignalR | Channel messages, DMs, attachments, reactions, read state, WebRTC signaling |
| Notification Service | Go 1.25 | Consumes events, persists notifications, serves read endpoints, pushes real-time updates via SSE |

### Storage

| Storage | Used by | Notes |
| --- | --- | --- |
| PostgreSQL | Auth, User, Guild, Notification | One database per service (no cross-service DB access) |
| ScyllaDB (Cassandra-compatible) | Chat | Wide-column store, partitioned by channel / conversation |

### Shared Infrastructure

| Technology | Purpose |
| --- | --- |
| RabbitMQ | Message queue for inter-service events (MassTransit on the .NET side) |
| NGINX | Reverse proxy + TLS termination (1443 HTTPS, 1080 HTTP to HTTPS redirect) |
| Docker + Docker Compose | Containerization, single-command deploy via root `compose.yaml` (composes per-service files) |
| Tilt | Local dev with file-watching live reload |
| SOPS + age (`tools/secretman`) | Encrypted per-service env files committed to Git |
| OpenTelemetry | Per-service SDK exposing a Prometheus `/metrics` endpoint (traces wired for forward-compatibility) |
| Prometheus | Metrics collection from every service |
| Grafana | Metrics dashboards |
| coturn | Self-hosted STUN/TURN server for WebRTC NAT traversal |
| MailHog | SMTP capture for confirmation emails (account deletion, data export ready); swapped for a real SMTP provider in prod |

### Why These Choices

- **Polyglot microservices.** Each service owns its stack. The communication contract (REST + RabbitMQ) is the only thing services need to agree on, so the internal tech of each service is an implementation detail and each owner picks what they're most productive in.
- **Next.js (frontend).** Built-in App Router, server components, image optimisation and a TypeScript-first DX. We use it as a frontend-only framework and pair it with separate backend services, which still qualifies as a Major per v21 III.3.
- **ASP.NET Core + Clean Architecture (Auth, Guild, Chat).** Strong typing, first-class SignalR for WebSockets, EF Core for Guild's relational model, and a layered structure (Domain / Application / Infrastructure / Persistence / Presentation) that keeps business rules off the framework. Carter handles minimal HTTP module routing on top of the standard ASP.NET pipeline.
- **Rust + axum + sqlx (User).** The User Service is read-heavy and concurrency-bound (friend lists, presence fan-out). Rust gives us a low-overhead async runtime and `sqlx` gives us compile-time-checked SQL.
- **Go (Gateway, Notification).** Both are small, latency-sensitive, mostly I/O-bound services. Go's HTTP stack and goroutines are an excellent fit, and the resulting binaries are tiny.
- **RabbitMQ + MassTransit.** Lightweight, easy to self-host, supports both pub/sub and work queues. MassTransit on the .NET side lets us treat queues as strongly typed message buses with automatic exchange / queue setup and contract-based routing.
- **PostgreSQL.** Default choice for relational data (Auth, User, Guild, Notification). One database per service enforces ownership boundaries.
- **ScyllaDB (Chat).** Chat is write-heavy and the natural access pattern is time-ordered by channel or conversation. A partitioned wide-column store handles this far more efficiently than a relational DB at scale; CQL compatibility lets us keep the schema versioned in `docs/schema/chat.cql`.
- **NGINX.** TLS termination only, plus an HTTP to HTTPS redirect. All authorisation and routing happens behind it in the Gateway.
- **SOPS + age (`tools/secretman`).** Encrypted env files live in the repo so contributors can decrypt their own copy without an out-of-band secret share. Per-service files keep blast radius small.
- **Tilt.** `tilt up` brings every service up with watch-mode rebuild, which is much faster than full `docker compose up --build` cycles during development.
- **OpenTelemetry + Prometheus + Grafana.** Each service is instrumented with the OpenTelemetry SDK and exposes a Prometheus `/metrics` endpoint via the SDK's Prometheus exporter; Prometheus scrapes it and Grafana visualises. Using OTel up-front keeps the door open for adding traces (and centralised logs) later without re-instrumenting every service.

---

## Database Schema

Full DDL lives in [`docs/schema/`](docs/schema/). Summary below.

| Service | Storage | Files |
| --- | --- | --- |
| Auth | PostgreSQL | [`docs/schema/auth.sql`](docs/schema/auth.sql) |
| User | PostgreSQL | [`docs/schema/user.sql`](docs/schema/user.sql) |
| Guild | PostgreSQL | [`docs/schema/guild.sql`](docs/schema/guild.sql) |
| Chat | **ScyllaDB** (CQL) | [`docs/schema/chat.cql`](docs/schema/chat.cql) |
| Notification | PostgreSQL | [`docs/schema/notification.sql`](docs/schema/notification.sql) |

### Key tables

| Table | Service | Notes |
| --- | --- | --- |
| `users_auth` | Auth | credentials, refresh token, soft-delete |
| `users_profile` | User | display info, avatar, banner, last-seen |
| `friendships` | User | pending / accepted state machine, canonical ordering constraint |
| `user_blocks` | User | unilateral blocks (independent of friendship) |
| `guilds` | Guild | name, icon, owner |
| `channel_categories` | Guild | collapsible channel groups |
| `channels` | Guild | text / announcement, NSFW flag, slowmode |
| `guild_members` | Guild | membership + per-guild nickname |
| `guild_bans` | Guild | separate from membership; supports pre-emptive bans |
| `guild_invites` | Guild | invite codes with expiry, use cap, revoke flag |
| `roles` | Guild | bitmask, position, hoisted, mentionable |
| `member_roles` | Guild | user to role per guild; composite FK prevents cross-guild assignment |
| `channel_permission_overwrites` | Guild | per-channel allow/deny by role or user |
| `messages` | Chat (ScyllaDB) | partitioned by `channel_id`, soft-delete, reply threading |
| `direct_messages` | Chat (ScyllaDB) | partitioned by `conversation_id` |
| `message_attachments` / `dm_attachments` | Chat (ScyllaDB) | file metadata per message |
| `message_reactions` / `message_reaction_counts` | Chat (ScyllaDB) | reactions + COUNTER totals |
| `channel_read_states` | Chat (ScyllaDB) | last-read position per user per channel |
| `user_conversations` | Chat (ScyllaDB) | DM thread index; `dm_unread_counts` for unread badges |
| `message_lookup` | Chat (ScyllaDB) | `message_id` to full PK + author_id; routes channel vs DM mutations |
| `notifications` | Notification | actor, source, expires, dismiss tracking |
| `notification_preferences` | Notification | per-user mute by guild or channel |

---

## Features List

| Feature | Description | Implemented by |
| --- | --- | --- |
| User registration | Email + password signup with hashed/salted passwords | `sliziard` |
| User login | JWT-based session, secure cookie handling | `sliziard` |
| OAuth 2.0 | Login with Google, GitHub or 42 | `sliziard` |
| User profiles | Avatar upload, display name, bio, status | `tstephan` |
| Friend system | Send/accept/decline friend requests, view friends list | `tstephan` |
| Online presence | Real-time online / idle / offline indicators | `tstephan` (User Service), `yandry` (Chat Service hub) |
| Guild creation | Create, edit, delete servers with icon and description | `yandry` |
| Channels | Create text channels and categories within a guild, manage order | `yandry` |
| Guild membership | Invite users, leave guilds, kick members | `yandry` |
| Role system | Create roles with color, position and permission bitmask | `yandry` |
| Permissions | Per-role access control with channel-level overwrites | `yandry` |
| Real-time chat | WebSocket-based messaging in channels and DMs via SignalR | `yandry`, `sliziard`, `achu` |
| Message history | Persistent chat history, paginated load | `yandry`, `sliziard`, `achu` |
| Direct messages | Private 1-on-1 real-time messaging | `yandry`, `sliziard`, `achu` |
| File uploads | Avatars, banners, chat attachments (multipart, type / size validation, MinIO storage) | `tstephan` (avatar/banner + frontend), `yandry` (chat attachments + MinIO container) |
| Notification system | Mentions, DM alerts, friend requests, real-time + persistent | `achu` |
| API Gateway | Centralized routing, JWT validation, rate limiting | `jramiro` |
| Message queue | RabbitMQ event bus between services | `yandry` |
| Monitoring | Prometheus metrics + Grafana dashboards for every service, fed by per-service OpenTelemetry exporters | `yandry`, `jramiro` |
| Containerization | Full Docker Compose setup, single-command launch | `yandry` |
| HTTPS | NGINX reverse proxy with TLS | `yandry` |
| Secret management | SOPS-encrypted per-service env files via `tools/secretman` | `jramiro` |
| Frontend | Next.js 16 App Router, routing, real-time updates | `tstephan`, `jramiro` |
| Multi-browser support | Tested and supported on Chrome (mandated), Firefox and Safari with broad feature parity | `tstephan`, `jramiro` |
| Server-Side Rendering | App Router renders Server Components by default for fast first paint and SEO | `tstephan`, `jramiro` |
| Design system | Tailwind tokens (palette, typography, spacing, shadows) + 11 reusable components, lucide-react icons | `tstephan`, `jramiro` |
| Voice calls | 1-on-1 real-time voice via WebRTC P2P | `yandry` (signaling), `tstephan` (frontend client + UI), `yandry` (coturn infra) |
| Video calls | 1-on-1 real-time video via WebRTC P2P | `yandry` (signaling), `tstephan` (frontend client + UI), `yandry` (coturn infra) |
| Privacy Policy | Accessible from footer, project-relevant content | `achu` |
| Terms of Service | Accessible from footer, project-relevant content | `achu` |
| GDPR: data export | "Download my data" button aggregates every service's data on the user into a single JSON bundle | all (cross-service) |
| GDPR: account deletion | `DELETE /auth/me` with confirmation flow + cross-service cascade (Auth, User, Guild, Chat, Notification consume `user.deleted`) + confirmation email | all (cross-service) |

---

## Modules

**Modules implemented: 27 points** - 14 baseline (2 Minor + 6 Major) + 13 bonus. Per chapter VII, at most 5 bonus points count toward the final grade, for a graded maximum of 19 (14 + 5).

> Note: per chapter IV, bonus points are only counted after the baseline 14 are fully validated.

### Baseline Modules (14pts)

| # | Module | Category | Type | Points | Implemented by |
| --- | --- | --- | --- | --- | --- |
| 1 | Frameworks for frontend and backend (Next.js + ASP.NET Core + axum) | Web | Major | 2 | `tstephan`, `jramiro` (Next.js frontend), `yandry`, `sliziard` (ASP.NET backends), `tstephan` (axum backend) |
| 2 | Real-time features (WebSockets via SignalR) | Web | Major | 2 | `yandry`, `sliziard`, `achu` |
| 3 | User Interaction (chat, profiles, friends, presence) | Web | Major | 2 | `tstephan` (profiles + friends), `yandry`, `sliziard`, `achu` (chat) |
| 4 | Backend as microservices | Devops | Major | 2 | `yandry` (architecture + infra), `jramiro` (API Gateway) |
| 5 | Standard user management (profiles, avatars, friends, online status) | User Management | Major | 2 | `tstephan` |
| 6 | Organization system (guilds with channels and membership) | User Management | Major | 2 | `yandry` |
| 7 | OAuth 2.0 remote authentication | User Management | Minor | 1 | `sliziard` |
| 8 | Notification system (mentions, DMs, friend requests) | Web | Minor | 1 | `achu` |

### Bonus Modules (13pts claimed, 5pt cap on graded contribution)

| # | Module | Category | Type | Points | Implemented by |
| --- | --- | --- | --- | --- | --- |
| 9 | Monitoring with Prometheus + Grafana | Devops | Major | 2 | `yandry`, `jramiro` |
| 10 | ORM (Entity Framework Core, Guild Service) | Web | Minor | 1 | `yandry` |
| 11 | Advanced permissions system (roles, hierarchy, overwrites) | User Management | Major | 2 | `yandry` |
| 12 | 1-on-1 voice and video calls (WebRTC) | Module of choice (IV.10) | Major | 2 | `yandry` (signaling + coturn infra), `tstephan` (frontend client + UI) |
| 13 | Custom design system (Tailwind tokens + 10+ reusable components) | Web | Minor | 1 | `tstephan`, `jramiro` |
| 14 | Server-Side Rendering (Next.js App Router) | Web | Minor | 1 | `tstephan`, `jramiro` |
| 15 | File upload and management (avatars, banners, chat attachments) | Web | Minor | 1 | `tstephan` (avatar/banner + frontend), `yandry` (chat attachments + MinIO container) |
| 16 | Multi-browser support (Firefox + Safari beyond mandated Chrome) | Accessibility | Minor | 1 | `tstephan`, `jramiro` |
| 17 | `tools/secretman` - SOPS-based encrypted env file workflow | Module of choice (IV.10) | Minor | 1 | `jramiro` |
| 18 | GDPR compliance (data export, deletion with confirmation, confirmation emails) | Data and Analytics | Minor | 1 | all (cross-service) |

### Module Justifications

**Module 1 - Frameworks for frontend and backend:**

The frontend is a full Next.js 16 application (App Router, server components, TypeScript). The backend services use multiple frameworks: ASP.NET Core 10 with Clean Architecture for the C# services (Auth, Guild, Chat) and axum for the Rust User Service. v21 III.3 explicitly accepts Next.js for this module even when paired with separate backends, which is our case. The Go services (Gateway, Notification) use Go's standard `net/http` library rather than a third-party framework; they exist to support the overall architecture but do not contribute to this module's framework claim.

**Module 2 - Real-time features (WebSockets):**

Real-time messaging, presence updates, typing indicators, and WebRTC call signaling are all delivered over SignalR (an abstraction over WebSockets that gracefully degrades to long-polling when needed). The Chat Service owns the hub and pushes events to per-channel and per-user SignalR groups based on membership changes published by the Guild Service.

**Module 3 - User Interaction:**

The application combines a chat experience, user profiles, and a friend system. Users can browse profiles, send and respond to friend requests, see online presence, exchange messages and reactions in shared guild channels, and have private 1-on-1 DMs. These features are exercised together rather than in isolation.

**Module 4 - Backend as microservices:**

Each service (Auth, User, Guild, Chat, Notification) has a single responsibility, exposes a versioned REST API and communicates asynchronously via RabbitMQ for cross-service events (e.g. `user.registered` triggers the User Service to create a profile and the Notification Service to send a welcome; `chat.message_sent` fans out to all connected clients via SignalR). Services are loosely coupled and independently deployable via Docker. Synchronous internal HTTP calls are reserved for the cases where a service needs an authoritative answer before it can act. These include Chat asking Guild whether the sender is a channel member with the right permission before accepting a message, Guild resolving a user's existence and profile against the User Service, Notification checking the block relationship between two users before creating a mention or DM notification, Auth querying Guild's owned-guilds-count before allowing an account deletion, and the User Service fanning out to every other service's `/internal/.../data-export` endpoint to assemble a GDPR export. Everything purely informational (a user went offline, a member joined a guild) flows asynchronously over RabbitMQ instead.

**Module 5 - Standard user management:**

The User Service owns profile data (display name, avatar, banner, status), the friend system (request / accept / decline / block) and online presence broadcasting. It exposes a complete REST API and publishes events when state changes for the rest of the system to consume.

**Module 6 - Organization system:**

Discord "servers" map directly to organizations: users create guilds, invite members, and organize communication into categories and channels. The Guild Service handles full CRUD for guilds, categories and channels, plus membership management (join via invite, leave, kick, ban).

**Module 7 - OAuth 2.0:**

The Auth Service implements the OAuth 2.0 Authorization Code flow with three providers: Google, GitHub, and 42. New OAuth identities are linked to an account (existing or newly created); a user may attach multiple providers to the same account. Tokens are exchanged server-side; the frontend never sees provider credentials.

**Module 8 - Notification system:**

The Notification Service is purely reactive: it consumes RabbitMQ events from Auth, Guild and Chat (`user.registered`, `friend.request_sent`, `chat.message_sent` with mention, `chat.dm_sent`, `call.incoming`, etc.), persists a notification row with actor / source / expiry, then pushes a real-time event to the recipient. Notifications survive disconnects (read state is server-side) and respect per-user mute preferences.

**Module 9 - Monitoring (Prometheus + Grafana):**

Each service exposes a `/metrics` endpoint scraped by Prometheus. Custom Grafana dashboards track request latency, error rates, active WebSocket connections, RabbitMQ queue depth and service health. Alerting rules notify via a webhook when error rates spike. Access to Grafana is password-protected.

**Module 10 - ORM (Entity Framework Core):**

The Guild Service uses EF Core 10 with a code-first model (Domain entities in `Guild.Domain`, configured in `Guild.Persistence`). EF Core handles relational queries, migrations and the unit-of-work pattern. Other services use lower-level data access tools by choice (sqlx for User, the Cassandra C# driver for Chat) so this module is explicitly scoped to Guild.

**Module 11 - Advanced permissions system:**

Each guild has a configurable role system. Roles carry a position (hierarchy), a permission bitmask, an optional color and hoist / mentionable flags. Permission resolution is co-located with guild membership in the Guild Service: a single in-memory pass over the caller's roles plus the channel's permission overwrites resolves the effective bitmask, with short-circuit on `ADMINISTRATOR`. Owners short-circuit higher than any role, role hierarchy gates role / member edits, and "you can't grant permissions you yourself lack" is enforced server-side.

**Module 12 - Voice and video calls (Module of choice, IV.10):**

1-on-1 real-time voice and video calling between users, implemented via WebRTC peer-to-peer connections. Call initiation and signaling (SDP offer / answer, ICE candidate exchange) are handled through the existing Chat Service SignalR hub, which means no extra service is required to coordinate calls. A STUN server handles NAT traversal for most network configurations, with a self-hosted coturn TURN server as fallback for restrictive networks.

*Why we claim this as a Major under IV.10:* this is substantial work (the WebRTC PeerConnection API, ICE negotiation, media-stream lifecycle, call-state machine, offline-callee notification and a full call UI on the frontend), it directly extends a core Discord feature complementing real-time chat, and the implementation touches three layers (Chat Service SignalR hub for signaling, frontend WebRTC client and call UI, coturn deployment for NAT traversal). The complexity and breadth justify treating it as a 2-point Major.

**Module 13 - Custom design system:**

The frontend ships with a dedicated design system rather than ad-hoc styling. The Tailwind configuration (`frontend/tailwind.config.ts`) defines a complete color palette (`primary-bg`, `secondary-bg`, `panel`, `frame`, `stroke`, plus accent colors `aqua`, `lavender`, `pink`, `orange`, `yellow`, `lime`), a 13-step typography scale with paired line heights, custom spacing tokens, and a signature glow shadow. Typography is driven by three Google fonts (Inter for body, Outfit for category labels, JetBrains Mono for code). Icons come from `lucide-react`. On top of the tokens we ship 11 reusable components in `frontend/src/components/` (`auth-card`, `channel-list`, `chat-message`, `chat-workspace`, `dm-list`, `friends-list`, `guild-member-list`, `guild-sidebar`, `notification-card`, `profile-card`, `settings-modal`), all composed from the design system. This satisfies the subject's "minimum 10 reusable components" plus palette / typography / icons requirement.

**Module 14 - Server-Side Rendering:**

The frontend uses Next.js 16's App Router, which renders every component as a React Server Component by default. Pages opt into client-side interactivity per file by adding `"use client"`; we do not opt in unless the component genuinely needs hooks or DOM-side event handlers, so static layouts and the initial render of dynamic pages are streamed pre-rendered HTML from the server. This improves first-contentful-paint, gives the markup to crawlers without JS execution (better SEO), and keeps the JavaScript bundle smaller because server-only logic never ships to the client. The subject explicitly flags SSR as incompatible with the ICP blockchain backend, which we do not use.

**Module 15 - File upload and management:**

Three upload surfaces back this module: user avatars (`POST /users/{id}/avatar`, multipart, JPEG / PNG / WebP, 5MB cap), profile banners (`POST /users/{id}/banner`, same constraints), and chat attachments (`POST /attachments` two-step flow, up to 10 per message, referenced by `attachment_ids` on `POST /channels/{id}/messages` and `POST /dms/{user_id}/messages`). Validation runs on both sides: the frontend rejects bad type or size before upload and shows progress, the backend re-validates and writes to a MinIO container that owns storage end-to-end so clients never see arbitrary URLs. Deletion is supported per surface (`DELETE /users/{id}/avatar` reverts to the default; deleted messages cascade their attachment refs). A cap of ten attachments per message, per-file type and size validation, and a background reaper that sweeps unreferenced draft uploads keep storage usage bounded. Attachments render inline in the chat view (image preview, click-through download for other types).

**Module 16 - Multi-browser support:**

Chrome is the subject's mandated baseline; we additionally support Firefox and Safari with broad feature parity. The frontend is built on Next.js + Tailwind which produce standards-compliant output that works cross-browser by default; we add explicit testing passes per browser plus per-browser CSS prefixing where needed. WebRTC behaviour gets dedicated attention since Safari historically diverges (autoplay rules, codec preferences, getUserMedia constraints), and any browser-specific quirks we cannot work around are documented as known limitations alongside the supported-browsers list in the project documentation.

**Module 17 - `tools/secretman` (Module of choice, IV.10):**

`tools/secretman` is a Go CLI that wraps SOPS + age to keep per-service secrets encrypted in Git rather than passed around out of band. It owns the full lifecycle: bootstrapping the age key for a new contributor, registering their public key in `.sops.yaml` with an authored comment, encrypting / decrypting / refreshing per-service env files, archiving + checksumming the encrypted bundles for distribution, and installing a BetterLeaks pre-commit hook so plaintext secrets are caught before they're committed. The team uses it every day for local-dev secret management; without it we would either commit plaintext secrets (a Hard-Requirement violation per chapter III) or coordinate secrets out-of-band, both of which scale poorly across a five-person project. We claim this as an IV.10 Minor because it is genuinely substantial Go code (multiple commands, key-management UX, hook installation, archive workflow) that solves a real recurring problem in our day-to-day collaboration, rather than a trivial helper script.

**Module 18 - GDPR compliance:**

The four subject bullets all map to concrete pieces of work:

- *Allow users to request their data + Export user data in a readable format.* Each service exposes an `/internal/users/{user_id}/data-export` endpoint that returns a JSON blob of the data that service holds about that user. The User Service hosts a public aggregator that fans out to Auth, Guild, Chat and Notification in parallel, stitches the results into a single bundle, and returns it as a downloadable JSON file. The frontend exposes a "Download my data" button in user settings.
- *Data deletion with confirmation.* `DELETE /auth/me` requires the frontend confirmation flow (`#150`) before firing; the Auth Service then publishes `user.deleted`, which Auth, User, Guild, Chat and Notification all consume (`#146`, `#147`, `#148`, `#149`) to purge that user's rows from their own databases. A synchronous Auth to Guild `/internal/users/{user_id}/owned-guilds-count` check (`#206`, already merged) blocks deletion if the user still owns guilds, forcing them to transfer ownership or delete those guilds first.
- *Confirmation emails for data operations.* A MailHog container in `compose.yaml` provides an SMTP endpoint for the eval environment. The Notification Service grows an email-sending side that consumes the `user.deleted` event (sends "Your account has been deleted") and a new `data.export_ready` event (sends "Your data export is ready, here is the download link"). For production deployments the MailHog container is swapped for a real SMTP provider; the Notification Service code stays the same.

---

## Individual Contributions

### `yandry` - Product Owner / Guild Service / Chat Service / Infrastructure

- Maintained and prioritized the product backlog, validated completed features before marking modules as done
- Designed the overall microservices architecture and inter-service communication contracts (REST + RabbitMQ event schemas like `user.registered`, `chat.message_sent`, `friend.request_sent`, `guild.member_joined`)
- Wrote the full Docker Compose setup (all services, databases, RabbitMQ, NGINX, Prometheus, Grafana, MinIO), configured NGINX reverse proxy and HTTPS/TLS, set up Tilt for local file-watching dev
- Set up RabbitMQ + MassTransit conventions across the .NET services
- Set up Prometheus scraping and built Grafana dashboards with alerting (with jramiro's help on the monitoring side), driven by per-service OpenTelemetry metric exporters
- Implemented the Guild Service (ASP.NET Core, Clean Architecture, EF Core): full CRUD for guilds, channel categories, channels, membership management (invite, join via link, kick, ban) and the full role and permissions system (role CRUD, role hierarchy, permission bitmasks, channel permission overwrites, permission resolution, plus the `/internal/users/{user_id}/owned-guilds-count` endpoint that gates Auth's account deletion)
- Led the Chat Service (ASP.NET Core + SignalR + ScyllaDB) alongside sliziard and achu: foundations, SignalR hub, channel message send/history, lazy channel subscription, DM send/history, WebRTC signaling (call offer / answer / ICE relay) and the coturn STUN/TURN deployment
- Added the MailHog container to the compose stack for GDPR confirmation emails, and implemented Guild + Chat's `/internal/users/{user_id}/data-export` endpoints and `user.deleted` cascade consumers

**Challenges:**

Most of the hard problems in my work came down to correctness under concurrency and across service boundaries, which is a polite way of saying "things that worked perfectly until two people clicked at once :D" The worst offender was the Guild Service's transactional outbox: domain events had to publish only if their database transaction committed, because a crash in the half-second between "row saved" and "event sent" silently desyncs every downstream service and never tells you it happened. Getting the MassTransit outbox to enlist in the same EF transaction and drain reliably took real iteration. Closely related was optimistic concurrency, where I map EF's `xmin` version tokens and duplicate-key insert races onto a clean `409 Conflict` instead of a `500`, so two clients editing the same guild or role resolve into correct semantics instead of a torn write.

The permission resolver was hard in a different way: effective access is a single in-memory pass over the caller's roles and channel overwrites, with owner and `ADMINISTRATOR` short-circuits, hierarchy gating on edits, and a "you can't grant a permission you don't hold yourself" rule, and making all those short-circuits compose correctly forced a consolidating refactor into `AuthorizationContext` + `PermissionResolver`. On the messaging side, cross-service events kept vanishing into `*_skipped` queues, MassTransit's serene way of telling you a message will never be delivered and declining to explain why, until I learned every consumer needs a matching `[MessageUrn]`; after that I single-sourced the snake_case policy so the envelope serialized identically over HTTP and MassTransit.

The data and real-time layers had their own traps. ScyllaDB partitions messages by channel (and DMs by conversation), so "give me everything belonging to one user" (read-state, DM lists, the GDPR export) is a full-cluster scan unless you plan for it, so I added per-user index tables and cursor-based pagination to keep those reads single-partition; Scylla is plenty fast, but only if you ask it questions the way it likes to be asked. WebRTC was the other one: driving 1-on-1 call setup over the SignalR hub with no media server meant running the whole ring/answer/hangup/timeout state machine myself, guarding against duplicate `call_id`s on a re-offer and scoping the timeout to the caller's leg so a call stops ringing into the void instead of forever. And holding all of it up, keeping one Compose stack running identically on both Docker and Podman (two container engines that agree on remarkably little: rootless quirks, port wiring, healthcheck differences) so anyone could bring the whole thing up with one command was its own quiet war of attrition.

---

### `tstephan` - Tech Lead / User Service / Frontend

- Held the Tech Lead seat for the project: chaired architecture discussions, validated technical proposals before they were turned into issues, owned the cross-cutting decisions (REST + RabbitMQ split, Snowflake `BIGINT` ID policy serialized as quoted strings on the wire, JWT validation at the Gateway rather than per-service, contracts-first development)
- Set and enforced code-quality standards across the team: PR-review discipline, Conventional Commits convention, shared linting/formatting rules, Husky pre-commit hooks with `lint-staged` + Prettier + ESLint on the frontend
- Designed the contracts-first workflow that splits "contract change" PRs from "feature implementation" PRs, so service boundaries stay reviewable in isolation
- Implemented the User Service in Rust (axum + sqlx + tokio): profile CRUD (display name, bio, status), avatar and banner upload with multipart validation and a default-avatar fallback, friend request state machine (pending to accepted / declined / blocked) with the canonical-ordering constraint on `friendships`, unilateral block flow independent of friendship, online / idle / offline presence published to RabbitMQ for the rest of the system to consume, `GET /users/search` with case-insensitive matching, batch user lookup (`POST /users` with `ids`), and the internal existence + relationship endpoints
- Built the frontend with jramiro on Next.js 16 (App Router, React 19, TypeScript, Tailwind 3, Zod, lucide-react): designed and owned the design system (color palette, 13-step typography scale, three custom Google fonts, custom spacing tokens, signature glow shadow) and shipped the 11 reusable components (`auth-card`, `channel-list`, `chat-message`, `chat-workspace`, `dm-list`, `friends-list`, `guild-member-list`, `guild-sidebar`, `notification-card`, `profile-card`, `settings-modal`) that the rest of the app composes
- Kept Server-Side Rendering as the default with jramiro by writing React Server Components everywhere except where client-side interactivity genuinely requires `"use client"`, keeping the JS bundle small and first-contentful-paint fast
- Implemented the WebRTC frontend on top of yandry's signaling: peer connection setup, ICE candidate exchange via the SignalR hub, media-stream lifecycle (camera / mic permissions, track replacement, hang-up cleanup), call UI / overlay, ringtone playback for incoming calls
- Enforced input validation on both sides of every form: Zod schemas on the client mirror the backend rules exactly, so the same error messages render in both places and the backend is never the only line of defense
- Implemented the User Service GDPR data-export aggregator (fans out to Auth, Guild, Chat and Notification's `/internal/users/{user_id}/data-export` endpoints in parallel, stitches the results into a single downloadable JSON bundle) and User's own per-service data-export endpoint; built the "Download my data" and "Delete my account" frontend flows with jramiro, including the destructive-action confirmation modal

**Challenges:** TBD

---

### `sliziard` - Project Manager / Auth Service / Chat co-developer

- Owned project management end-to-end: ran sprint planning and grooming sessions, populated and maintained "The Holy Board" on GitHub Projects across five sprints, broke epics down into well-scoped issues, tracked iteration progress and blockers, drove retros, kept the team's communication rhythm on Discord
- Set up the Auth Service from scaffolding to production-shaped Clean Architecture: `Auth.Domain` (entities, value objects like `Email` and `OAuthIdentity`, failure catalogue), `Auth.Application` (command/query handlers, abstractions), `Auth.Infrastructure` (JWT issuance, OAuth provider clients, password hasher), `Auth.Persistence` (EF Core mappings, migrations), `Auth.Presentation` (Carter endpoints, global exception middleware)
- Implemented the email + password auth flows: `POST /auth/register` and `POST /auth/login` with `bcrypt`-hashed + salted passwords, `POST /auth/refresh` with refresh-token rotation and hashed storage of refresh tokens, `POST /auth/logout` with refresh-token revocation, `GET /auth/me` for the authenticated principal's auth-side identity, and soft-delete on `users_auth` so emails free up correctly after deletion
- Implemented the full OAuth 2.0 Authorization Code flow for three providers (Google, GitHub, 42) under `GET /auth/oauth/{provider}` and `GET /auth/oauth/{provider}/callback`, plus the integer-encoded `oauth_provider` storage decision (`Github=1`, `Google=2`, `FortyTwo=3`) so the schema stays compact and migrations don't churn on enum renames
- Building the OAuth link / unlink flow for multi-provider accounts (one account, multiple linked OAuth identities, none allowed to leave the account without a password set)
- Implemented the synchronous Auth to Guild `/internal/users/{user_id}/owned-guilds-count` check that gates `DELETE /auth/me` and forces users to transfer or delete owned guilds before account deletion; published the `user.deleted` event that drives the GDPR deletion cascade across User, Guild, Chat and Notification
- Added Auth's `/internal/users/{user_id}/data-export` endpoint for the GDPR data-export bundle (email, OAuth providers, account timestamps), validated that OAuth-only accounts cannot be tricked into changing their email or password via `/auth/me`
- Co-developer on the Chat Service alongside yandry and achu, contributing to the SignalR hub, message flows and consumer wiring as part of the cross-team push to round out the chat experience

**Challenges:** TBD

---

### `jramiro` - API Gateway / Secret Management / Frontend / Monitoring

- Implemented the API Gateway in Go end-to-end: local JWT verification via `golang-jwt` (no per-request hop to Auth on the hot path), two stacked rate-limiting layers (per-IP and per-UID, backed by an in-memory store with TTL eviction), schema-aware route table that fetches the current OpenAPI of each downstream service to validate incoming requests, timeout middleware, `/metrics` passthrough for Prometheus scraping, healthcheck endpoint, and the `/api/{service}/vN/...` reverse-proxy convention that the rest of the frontend depends on
- Fixed the gateway schema-retrieval logic bug (`#130`) that was masking validation failures on services with multiple route prefixes
- Built the `tools/secretman` Go CLI for SOPS-encrypted per-service env files: ensure / encrypt / decrypt / refresh workflows, age-binary extraction and pinning, age key bootstrapping for new contributors with their public key auto-registered in `.sops.yaml` (with a `# user@hostname` authored comment), archive + checksum helpers for distributing encrypted bundles, plaintext-path masking in user-facing output, and a BetterLeaks pre-commit hook installer that catches plaintext-secret commits before they leave the workstation
- Maintained the Tiltfile network and port wiring across services (`#162`) so `tilt up` brings everything up without port conflicts on contributors' machines
- Co-developed the frontend with tstephan on Next.js 16 (App Router, React Server Components, design system, Tailwind, lucide-react), shipping pages and components that consume the design system tokens rather than reinventing styling
- Partnered with yandry on the monitoring stack: helped wire each service's OpenTelemetry metric exporter, configured Prometheus scrape targets, contributed Grafana dashboards and alerting rules
- Built the "Download my data" and "Delete my account" GDPR frontend flows with tstephan, including the confirmation modal and the streaming JSON download

**Challenges:** TBD

---

### `achu` - Notification Service / Chat co-developer / UX

- Implemented the Notification Service in Go from scaffolding to production: PostgreSQL-backed persistence with the `notifications` table (actor, source, type, expiry, dismiss tracking) and `notification_preferences` table for per-user mute (per-guild and per-channel granularity)
- Wired all five upstream consumers: `user.registered` (welcome notification), `friend.request_sent` (friend_request type), `chat.message_sent` with mention detection (mention type), `chat.dm_sent` (dm type), `call.incoming` (incoming_call type), and `user.deleted` (cascade purge of the user's notifications and preferences)
- Built the real-time delivery layer: pushed notifications to connected clients over a Server-Sent Events stream (`GET /notifications/events`), emitting a named `ReceiveNotification` event that updates navbar badges without polling
- Built the REST surface (`GET /notifications`, `POST /notifications/{id}/read`, mute preference endpoints) plus the background cleanup for old dismissed notifications (`#171`)
- Wired mention block-suppression to the User Service's internal endpoint (`#191`) so users who have blocked each other do not trigger mention notifications
- Implemented Notification's `/internal/users/{user_id}/data-export` endpoint and the email-sending side of the Notification Service: consumes `user.deleted` and `data.export_ready` events, renders the corresponding confirmation emails (plaintext + minimal HTML templates) and dispatches them through MailHog locally / a real SMTP provider in production
- Co-developer on the Chat Service alongside yandry and sliziard, contributing to message flows, presence wiring and consumer setup
- Wore the UX hat for the project: designed the notification card, notification bell badge, mute-preference settings panel, did a mobile-responsiveness pass on the main views, and ran the zero-console-errors / accessibility audits against the latest stable Chrome
- Wrote the Privacy Policy and Terms of Service pages with real, project-relevant content (not lorem-ipsum placeholders), reachable from the footer on every page

**Challenges:** TBD
