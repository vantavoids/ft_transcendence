-- User Service — PostgreSQL schema
-- Owns: profiles, avatars, friend relationships, blocks, online status

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

CREATE TYPE user_status AS ENUM ('online', 'idle', 'dnd', 'offline');

CREATE TABLE users_profile (
    id              UUID        PRIMARY KEY,   -- same UUID as users_auth.id
    username        VARCHAR(32) UNIQUE NOT NULL,
    display_name    VARCHAR(64),
    avatar_url      VARCHAR(512),
    banner_url      VARCHAR(512),
    status          user_status NOT NULL DEFAULT 'offline',
    -- last_seen_at is updated when status transitions to 'offline'
    -- used for "last seen X ago" display on offline users
    last_seen_at    TIMESTAMPTZ,
    bio             TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- -----------------------------------------------------------------------
-- Friend relationships
--
-- Canonical ordering: requester_id < addressee_id (enforced by constraint).
-- This prevents (A→B) and (B→A) ghost duplicates.
-- To check "are A and B friends", query with: (min(A,B), max(A,B)).
-- -----------------------------------------------------------------------

CREATE TYPE friendship_status AS ENUM ('pending', 'accepted', 'blocked');

CREATE TABLE friendships (
    id              UUID                PRIMARY KEY DEFAULT gen_random_uuid(),
    requester_id    UUID                NOT NULL REFERENCES users_profile (id) ON DELETE CASCADE,
    addressee_id    UUID                NOT NULL REFERENCES users_profile (id) ON DELETE CASCADE,
    status          friendship_status   NOT NULL DEFAULT 'pending',
    created_at      TIMESTAMPTZ         NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ         NOT NULL DEFAULT NOW(),

    CONSTRAINT no_self_friendship   CHECK (requester_id != addressee_id),
    -- enforce canonical ordering so (A,B) and (B,A) cannot both exist
    CONSTRAINT canonical_pair       CHECK (requester_id < addressee_id),
    CONSTRAINT unique_friendship    UNIQUE (requester_id, addressee_id)
);

CREATE INDEX idx_friendships_requester          ON friendships (requester_id);
CREATE INDEX idx_friendships_addressee          ON friendships (addressee_id);
-- hot path: "show pending friend requests I've received"
CREATE INDEX idx_friendships_pending_addressee  ON friendships (addressee_id)
    WHERE status = 'pending';

-- -----------------------------------------------------------------------
-- User blocks (unilateral — independent of friendship status)
--
-- A blocks B: blocker_id = A, blocked_id = B.
-- B is not notified. B can still send messages; the frontend filters them out.
-- Guild Service / Chat Service honour blocks by checking this table via HTTP.
-- -----------------------------------------------------------------------

CREATE TABLE user_blocks (
    blocker_id  UUID        NOT NULL REFERENCES users_profile (id) ON DELETE CASCADE,
    blocked_id  UUID        NOT NULL REFERENCES users_profile (id) ON DELETE CASCADE,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    PRIMARY KEY (blocker_id, blocked_id),
    CONSTRAINT no_self_block CHECK (blocker_id != blocked_id)
);

-- fast check: "has user X blocked me?" (needed before sending a DM)
CREATE INDEX idx_user_blocks_blocked ON user_blocks (blocked_id);
