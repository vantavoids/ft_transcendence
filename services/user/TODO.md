# User Service TODO

Backlog for unit and integration tests in `services/user`.

## Priority 1

- [ ] `PATCH /v1/users/{id}` returns `400` for `id <= 0`
- [ ] `PATCH /v1/users/{id}` returns `403` when `sub != path id`
- [ ] `PATCH /v1/users/{id}` returns `400` for invalid JSON body
- [ ] `PATCH /v1/users/{id}` rejects `avatar_url` in the body with `400`
- [ ] `PATCH /v1/users/{id}` rejects `banner_url` in the body with `400`
- [ ] `PATCH /v1/users/{id}` returns `400` when `display_name` exceeds 64 chars
- [ ] `PATCH /v1/users/{id}` returns `404` when the profile does not exist
- [ ] `PATCH /v1/users/{id}` returns `200` with the updated profile on the happy path
- [ ] `GET /v1/users/me` returns `401` for missing or invalid JWT
- [ ] `GET /v1/users/me` returns `200` with the expected profile shape
- [ ] `GET /v1/users/{id}` returns `401` for missing or invalid JWT
- [ ] `GET /v1/users/{id}` returns `404` when the profile does not exist
- [ ] `GET /v1/users/{id}` returns `200` with `id` serialized as a string

## Priority 2

- [ ] `caller_id_from_headers` accepts `Bearer <token>`
- [ ] `caller_id_from_headers` accepts `bearer <token>`
- [ ] `caller_id_from_headers` rejects tokens with fewer than 3 segments
- [ ] `caller_id_from_headers` rejects tokens with more than 3 segments
- [ ] `caller_id_from_headers` rejects invalid base64 payloads
- [ ] `caller_id_from_headers` rejects missing `sub`
- [ ] `caller_id_from_headers` rejects non-numeric `sub`
- [ ] `caller_id_from_headers` rejects `sub <= 0`

## Priority 3

- [ ] `UpdateUserRequest` accepts an empty object `{}`
- [ ] `UpdateUserRequest` accepts `display_name` only
- [ ] `UpdateUserRequest` accepts `bio` only
- [ ] `UpdateUserRequest` accepts `status` values: `online`, `idle`, `dnd`, `offline`
- [ ] `UpdateUserRequest` rejects unknown `status` values
- [ ] `UpdateUserRequest` rejects unknown fields because of `deny_unknown_fields`
- [ ] `UserProfile.id` is serialized as a string
- [ ] `UserSummary.id` is serialized as a string
- [ ] `last_seen_at` is serialized in ISO-8601 when present

## Priority 4

- [ ] `fetch_user_profile` returns `None` for unknown IDs
- [ ] `fetch_user_profile` returns `avatar_url`, `banner_url`, `bio`, and `last_seen_at`
- [ ] `update_user_profile` preserves untouched fields
- [ ] `update_user_profile` updates `updated_at`
- [ ] `update_user_profile` returns `None` for unknown IDs
- [ ] `fetch_user_summaries` preserves input order
- [ ] `fetch_user_summaries` filters blocked users
- [ ] `fetch_user_summaries` ignores unknown IDs

## Priority 5

- [ ] `main.rs` exposes `GET /v1/users/me`
- [ ] `main.rs` exposes `GET /v1/users/{id}`
- [ ] `main.rs` exposes `PATCH /v1/users/{id}`
- [ ] OpenAPI output includes the new routes
- [ ] Existing `GET /v1/users` behavior stays unchanged

## Priority 6

- [ ] `display_name` at exactly 64 chars is accepted
- [ ] `bio` can be cleared explicitly if that is the intended contract
- [ ] A `PATCH` with only `status` does not modify `display_name` or `bio`
- [ ] A second `PATCH` keeps the expected final profile state
- [ ] A `GET` after `PATCH` returns the persisted values

## Test Support

- [ ] Add fixtures for a seeded profile
- [ ] Add helper for authenticated request headers
- [ ] Add helper for serializing JSON payloads
- [ ] Add helper for seeding JWT claims in tests
