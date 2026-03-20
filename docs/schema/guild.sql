-- Guild Service — PostgreSQL schema
-- Owns: guilds, channel categories, channels, membership, bans, invite codes
--
-- Cross-service note: user_id columns are logical refs — User Service owns user records.
-- Guild Service verifies user existence via HTTP before inserting any user_id.

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

CREATE TABLE guilds (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    name            VARCHAR(100) NOT NULL,
    description     TEXT,
    icon_url        VARCHAR(512),
    banner_url      VARCHAR(512),
    owner_id        UUID        NOT NULL,   -- logical ref → users_profile.id
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_guilds_owner ON guilds (owner_id);

-- -----------------------------------------------------------------------
-- Channel categories (collapsible sections in the sidebar, like Discord)
-- -----------------------------------------------------------------------

CREATE TABLE channel_categories (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    guild_id    UUID        NOT NULL REFERENCES guilds (id) ON DELETE CASCADE,
    name        VARCHAR(100) NOT NULL,
    position    INT         NOT NULL DEFAULT 0,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_categories_guild ON channel_categories (guild_id, position);

-- -----------------------------------------------------------------------
-- Channels
-- -----------------------------------------------------------------------

CREATE TYPE channel_type AS ENUM ('text', 'announcement', 'voice');

CREATE TABLE channels (
    id              UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    guild_id        UUID            NOT NULL REFERENCES guilds (id) ON DELETE CASCADE,
    -- NULL = uncategorised (shown above all categories)
    category_id     UUID            REFERENCES channel_categories (id) ON DELETE SET NULL,
    name            VARCHAR(100)    NOT NULL,
    type            channel_type    NOT NULL DEFAULT 'text',
    topic           TEXT,
    position        INT             NOT NULL DEFAULT 0,
    is_nsfw         BOOLEAN         NOT NULL DEFAULT FALSE,
    -- minimum seconds between messages per user (0 = no slowmode)
    slowmode_seconds INT            NOT NULL DEFAULT 0,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_channels_guild    ON channels (guild_id, position);
CREATE INDEX idx_channels_category ON channels (category_id, position);

-- -----------------------------------------------------------------------
-- Guild membership
-- -----------------------------------------------------------------------

CREATE TABLE guild_members (
    guild_id    UUID        NOT NULL REFERENCES guilds (id) ON DELETE CASCADE,
    user_id     UUID        NOT NULL,   -- logical ref → users_profile.id
    -- per-guild display name override (NULL = use users_profile.display_name)
    nickname    VARCHAR(64),
    joined_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (guild_id, user_id)
);

CREATE INDEX idx_guild_members_user ON guild_members (user_id);

-- -----------------------------------------------------------------------
-- Guild bans (separate from membership for pre-emptive bans + clean audit)
-- A banned user may never have been a member (pre-emptive ban).
-- -----------------------------------------------------------------------

CREATE TABLE guild_bans (
    guild_id    UUID        NOT NULL REFERENCES guilds (id) ON DELETE CASCADE,
    user_id     UUID        NOT NULL,   -- logical ref → users_profile.id
    banned_by   UUID        NOT NULL,   -- logical ref → users_profile.id
    reason      TEXT,
    -- NULL = permanent ban
    expires_at  TIMESTAMPTZ,
    banned_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (guild_id, user_id)
);

CREATE INDEX idx_guild_bans_user ON guild_bans (user_id);

-- -----------------------------------------------------------------------
-- Guild invites
-- -----------------------------------------------------------------------

CREATE TABLE guild_invites (
    code        VARCHAR(16)  PRIMARY KEY,
    guild_id    UUID         NOT NULL REFERENCES guilds (id) ON DELETE CASCADE,
    created_by  UUID         NOT NULL,   -- logical ref → users_profile.id
    expires_at  TIMESTAMPTZ,             -- NULL = never expires
    max_uses    INT,                     -- NULL = unlimited
    uses        INT          NOT NULL DEFAULT 0,
    is_revoked  BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_guild_invites_guild ON guild_invites (guild_id)
    WHERE is_revoked = FALSE;
