# User Service NestJS Migration Todo

Branch: `feat/user-service-nestjs`

Status legend:
- `[ ]` not started
- `[-]` in progress
- `[x]` done

Delivery order is dependency-first.

## Phase 1 - Contract and runtime shell

- [ ] Replace the Rust runtime with a NestJS application
  - [ ] add `package.json`, `tsconfig.json`, and build scripts
  - [ ] add the Nest bootstrap, app module, and global validation
  - [ ] expose `GET /healthz`
- [ ] Align the database bootstrap with the target schema
  - [ ] replace the legacy UUID init SQL with the `BIGINT`/Snowflake schema
  - [ ] keep `users_profile`, `friendships`, and `user_blocks` as the initial tables
- [ ] Update the service image and local dev plumbing
  - [ ] replace the Rust Dockerfiles with Node.js builds
  - [ ] update Tilt and compose to start the Nest app
  - [ ] refresh ignore files and lockfile generation

## Phase 2 - Internal endpoints

- [ ] Implement `GET /internal/users/{user_id}`
  - [ ] return the public profile shape
  - [ ] return `404` when the profile does not exist
- [ ] Implement `GET /internal/users/{user_id}/relationship-with/{other_user_id}`
  - [ ] resolve block state first
  - [ ] resolve friendship state second
  - [ ] return the documented `status` / `since` payload

## Phase 3 - Public profile surface

- [ ] Implement `GET /users/me`
- [ ] Implement `GET /users/{id}`
- [ ] Implement `PATCH /users/{id}`
- [ ] Implement `GET /users`
- [ ] Implement `GET /users/search`

## Phase 4 - Social graph

- [ ] Implement friend endpoints
- [ ] Implement block endpoints
- [ ] Add RabbitMQ events for relationship changes

## Phase 5 - Media

- [ ] Implement avatar upload / delete
- [ ] Implement banner upload / delete
- [ ] Add storage integration for profile media

