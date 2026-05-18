CREATE KEYSPACE IF NOT EXISTS chat
    WITH replication = {
        'class': 'NetworkTopologyStrategy',
        'replication_factor': 1   -- bump to 3 in production
    }
    AND durable_writes = true
    -- Tablets are enabled by default in Scylla 6.x but don't yet support
    -- COUNTER columns, which message_reaction_counts and dm_unread_counts
    -- rely on. Disable until Scylla closes that gap.
    AND tablets = {'enabled': false};

USE chat;

CREATE TABLE IF NOT EXISTS messages (
    channel_id  BIGINT,
    created_at  TIMESTAMP,
    id          BIGINT,
    author_id   BIGINT,
    -- NULL for attachment-only messages
    content     TEXT,
    edited_at   TIMESTAMP,
    -- soft delete: set is_deleted = true and NULL out content on delete
    is_deleted  BOOLEAN,
    -- reply threading: NULL = top-level message
    reply_to_id BIGINT,
    PRIMARY KEY ((channel_id), created_at, id)
) WITH CLUSTERING ORDER BY (created_at DESC, id DESC)
   AND comment = 'Channel message history, partitioned per channel';

CREATE TABLE IF NOT EXISTS direct_messages (
    conversation_id BIGINT,
    created_at      TIMESTAMP,
    id              BIGINT,
    sender_id       BIGINT,
    recipient_id    BIGINT,
    content         TEXT,
    edited_at       TIMESTAMP,
    is_deleted      BOOLEAN,
    reply_to_id     BIGINT,
    PRIMARY KEY ((conversation_id), created_at, id)
) WITH CLUSTERING ORDER BY (created_at DESC, id DESC)
   AND comment = 'DM history, partitioned per conversation pair';

CREATE TABLE IF NOT EXISTS message_attachments (
    channel_id  BIGINT,
    message_id  BIGINT,
    id          BIGINT,
    url         TEXT,
    filename    TEXT,
    size_bytes  BIGINT,
    mime_type   TEXT,
    PRIMARY KEY ((channel_id, message_id), id)
) WITH comment = 'File attachment metadata per channel message';

CREATE TABLE IF NOT EXISTS dm_attachments (
    conversation_id BIGINT,
    message_id      BIGINT,
    id              BIGINT,
    url             TEXT,
    filename        TEXT,
    size_bytes      BIGINT,
    mime_type       TEXT,
    PRIMARY KEY ((conversation_id, message_id), id)
) WITH comment = 'File attachment metadata per DM';

CREATE TABLE IF NOT EXISTS draft_attachments (
    id          BIGINT PRIMARY KEY,
    uploader_id BIGINT,
    url         TEXT,
    filename    TEXT,
    size_bytes  BIGINT,
    mime_type   TEXT,
    created_at  TIMESTAMP
) WITH default_time_to_live = 3600
   AND comment = 'Pre-message attachment uploads; auto-expires in 1h';

CREATE TABLE IF NOT EXISTS attachment_lookup (
    attachment_id   BIGINT PRIMARY KEY,
    -- routing
    is_dm           BOOLEAN,
    -- for channel attachments (is_dm = false)
    channel_id      BIGINT,
    -- for DM attachments (is_dm = true)
    conversation_id BIGINT,
    -- common
    message_id      BIGINT
) WITH comment = 'Maps attachment_id -> owning message PK for REST downloads';

CREATE TABLE IF NOT EXISTS message_reactions (
    channel_id  BIGINT,
    message_id  BIGINT,
    emoji       TEXT,
    user_id     BIGINT,
    created_at  TIMESTAMP,
    PRIMARY KEY ((channel_id, message_id), emoji, user_id)
) WITH comment = 'Per-user reaction records; used to check if the current user already reacted';

CREATE TABLE IF NOT EXISTS message_reaction_counts (
    channel_id  BIGINT,
    message_id  BIGINT,
    emoji       TEXT,
    count       COUNTER,
    PRIMARY KEY ((channel_id, message_id), emoji)
) WITH comment = 'Reaction totals per emoji; COUNTER for atomic increments';

CREATE TABLE IF NOT EXISTS channel_read_states (
    user_id              BIGINT,
    channel_id           BIGINT,
    last_read_message_id BIGINT,
    last_read_at         TIMESTAMP,
    PRIMARY KEY ((user_id), channel_id)
) WITH comment = 'Per-user last-read position per channel';

CREATE TABLE IF NOT EXISTS user_conversations (
    user_id          BIGINT,
    partner_id       BIGINT,
    conversation_id  BIGINT,
    last_message_at  TIMESTAMP,
    -- first ~100 chars of the most recent message for the DM sidebar preview
    last_preview     TEXT,
    is_archived      BOOLEAN,
    PRIMARY KEY ((user_id), partner_id)
) WITH comment = 'DM conversation index per user; sort by last_message_at in application layer';

CREATE TABLE IF NOT EXISTS dm_unread_counts (
    user_id     BIGINT,
    partner_id  BIGINT,
    count       COUNTER,
    PRIMARY KEY ((user_id), partner_id)
) WITH comment = 'Unread DM message count per conversation; reset on read';

CREATE TABLE IF NOT EXISTS message_lookup (
    message_id       BIGINT PRIMARY KEY,
    -- routing
    is_dm            BOOLEAN,
    -- for channel messages (is_dm = false)
    channel_id       BIGINT,
    -- for DMs (is_dm = true)
    conversation_id  BIGINT,
    -- common
    created_at       TIMESTAMP,
    author_id        BIGINT
) WITH comment = 'Auxiliary lookup: message_id → full PK + author_id for REST mutations';
