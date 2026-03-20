-- Permissions Service — PostgreSQL schema
-- Owns: roles, permission bitmasks, role assignments, channel overwrites
--
-- Cross-service note: guild_id and channel_id are logical refs.
-- Permissions Service verifies their existence via HTTP when creating roles/overwrites.

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- -----------------------------------------------------------------------
-- Permission bitmask constants (enforced in application code)
-- -----------------------------------------------------------------------
-- SEND_MESSAGES        = 1 <<  0  =       1
-- READ_MESSAGES        = 1 <<  1  =       2
-- MANAGE_MESSAGES      = 1 <<  2  =       4   (edit/delete others' messages)
-- MANAGE_CHANNELS      = 1 <<  3  =       8
-- KICK_MEMBERS         = 1 <<  4  =      16
-- BAN_MEMBERS          = 1 <<  5  =      32
-- MANAGE_ROLES         = 1 <<  6  =      64
-- MANAGE_GUILD         = 1 <<  7  =     128   (edit guild name/icon/etc.)
-- ADMINISTRATOR        = 1 <<  8  =     256   (bypasses all permission checks)
-- CREATE_INVITE        = 1 <<  9  =     512
-- MENTION_EVERYONE     = 1 << 10  =    1024
-- MANAGE_NICKNAMES     = 1 << 11  =    2048   (change others' guild nicknames)
-- -----------------------------------------------------------------------

CREATE TABLE roles (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    guild_id        UUID        NOT NULL,   -- logical ref → guilds.id
    name            VARCHAR(64) NOT NULL,
    color           VARCHAR(7),            -- hex, e.g. '#5865F2'; NULL = no colour
    permissions     BIGINT      NOT NULL DEFAULT 0,
    -- position determines display order and precedence: higher = more powerful
    -- DEFERRABLE allows swapping two positions in a single transaction
    position        INT         NOT NULL DEFAULT 0,
    is_default      BOOLEAN     NOT NULL DEFAULT FALSE,  -- true = @everyone role
    is_hoisted      BOOLEAN     NOT NULL DEFAULT FALSE,  -- show separately in member list
    is_mentionable  BOOLEAN     NOT NULL DEFAULT FALSE,  -- allow @role mentions in messages
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT unique_role_name_per_guild   UNIQUE (guild_id, name),
    -- expose (guild_id, id) as a unique pair so member_roles can FK to it
    -- and guarantee cross-guild role assignment is impossible at the DB level
    CONSTRAINT roles_guild_id_uk            UNIQUE (guild_id, id),
    -- role positions must be unique within a guild (deferrable for bulk reorders)
    CONSTRAINT unique_role_position         UNIQUE (guild_id, position) DEFERRABLE INITIALLY DEFERRED
);

CREATE INDEX idx_roles_guild         ON roles (guild_id, position DESC);
-- fast @everyone lookup during every permission resolution
CREATE INDEX idx_roles_guild_default ON roles (guild_id) WHERE is_default = TRUE;

-- enforce exactly one @everyone role per guild at the index level
CREATE UNIQUE INDEX idx_one_default_role_per_guild ON roles (guild_id)
    WHERE is_default = TRUE;

-- -----------------------------------------------------------------------
-- Role assignments (member ↔ role, many-to-many per guild)
-- The composite FK on (guild_id, role_id) → roles (guild_id, id) guarantees
-- that a role from guild A can never be assigned to a member of guild B.
-- -----------------------------------------------------------------------

CREATE TABLE member_roles (
    guild_id    UUID        NOT NULL,
    user_id     UUID        NOT NULL,   -- logical ref → users_profile.id
    role_id     UUID        NOT NULL,
    assigned_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    PRIMARY KEY (guild_id, user_id, role_id),

    -- cross-guild assignment guard
    FOREIGN KEY (guild_id, role_id) REFERENCES roles (guild_id, id) ON DELETE CASCADE
);

CREATE INDEX idx_member_roles_user ON member_roles (user_id, guild_id);
CREATE INDEX idx_member_roles_role ON member_roles (role_id);

-- -----------------------------------------------------------------------
-- Per-channel permission overrides
-- allow / deny bitmasks override the role's base permissions for one channel.
-- guild_id is denormalised here so the Permissions Service can bulk-delete
-- all overwrites when a guild is deleted (via RabbitMQ event) without needing
-- to join the Guild Service's channels table.
-- -----------------------------------------------------------------------

CREATE TABLE channel_permission_overwrites (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    guild_id    UUID        NOT NULL,   -- denormalised for bulk cleanup
    channel_id  UUID        NOT NULL,   -- logical ref → channels.id
    target_id   UUID        NOT NULL,   -- role_id or user_id
    target_type VARCHAR(8)  NOT NULL CHECK (target_type IN ('role', 'user')),
    allow       BIGINT      NOT NULL DEFAULT 0,
    deny        BIGINT      NOT NULL DEFAULT 0,

    CONSTRAINT unique_overwrite UNIQUE (channel_id, target_id, target_type)
);

CREATE INDEX idx_overwrites_channel ON channel_permission_overwrites (channel_id);
CREATE INDEX idx_overwrites_guild   ON channel_permission_overwrites (guild_id);
