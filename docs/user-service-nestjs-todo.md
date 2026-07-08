# User Service NestJS Migration Todo

Branch: `feat/user-service-nestjs`

Status legend:
- `[ ]` not started
- `[-]` in progress
- `[x]` done

Delivery order is dependency-first.

## Phase 1 - Contract and runtime shell

- [x] Replace the Rust runtime with a NestJS application
  - [x] add `package.json`, `tsconfig.json`, and build scripts
  - [x] add the Nest bootstrap, app module, and global validation
  - [x] expose `GET /healthz`
- [x] Align the database bootstrap with the target schema
  - [x] replace the legacy UUID init SQL with the `BIGINT`/Snowflake schema
  - [x] keep `users_profile`, `friendships`, and `user_blocks` as the initial tables
- [x] Update the service image and local dev plumbing
  - [x] replace the Rust Dockerfiles with Node.js builds
  - [x] update Tilt and compose to start the Nest app
  - [x] refresh ignore files and lockfile generation

## Phase 2 - Internal endpoints

- [x] Implement `GET /internal/users/{user_id}`
  - [x] return the public profile shape
  - [x] return `404` when the profile does not exist
- [x] Implement `GET /internal/users/{user_id}/relationship-with/{other_user_id}`
  - [x] resolve block state first
  - [x] resolve friendship state second
  - [x] return the documented `status` / `since` payload

## Phase 3 - Public profile surface

- [x] Implement `GET /users/me`
- [x] Implement `GET /users/{id}`
- [x] Implement `PATCH /users/{id}`
- [x] Implement `GET /users`
- [x] Implement `GET /users/search`

## Phase 4 - Social graph

- [x] Implement friend endpoints
- [x] Implement block endpoints
- [x] Add RabbitMQ events for relationship changes

## Phase 5 - Media

- [x] Implement avatar upload / delete
- [x] Implement banner upload / delete
- [x] Add storage integration for profile media
