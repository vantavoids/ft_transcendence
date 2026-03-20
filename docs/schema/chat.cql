-- Chat Service — ScyllaDB schema (Cassandra-compatible CQL)
-- Owns: channel messages, DMs, reactions, attachments, read states, conversation index
--
-- CQL notes:
--   - Non-PK columns are always nullable; NOT NULL is implicit only for PK components.
--   - Table options: first option uses WITH, subsequent options use AND.
--   - Partition sizing: messages are partitioned by channel_id. For very high-volume
--     channels, add a time bucket: PRIMARY KEY ((channel_id, bucket), created_at, id)
--     where bucket = YYYYMM. Acceptable as-is for a 42 project.
--   - Mutable columns: always use INSERT (idempotent upsert) for regular tables.
--     Counters must use UPDATE on dedicated COUNTER tables (CQL constraint).
--   - Pagination: use `created_at < :before_time` in WHERE clause. Never use OFFSET.

CREATE KEYSPACE IF NOT EXISTS chat
    WITH replication = {
        'class': 'NetworkTopologyStrategy',
        'replication_factor': 1   -- bump to 3 in production
    }
    AND durable_writes = true;

USE chat;

-- ---------------------------------------------------------------------------
-- Channel messages
-- Partition key  : channel_id
-- Clustering key : created_at DESC, id DESC (newest first; id breaks ms ties)
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS messages (
    channel_id  UUID,
    created_at  TIMESTAMP,
    id          UUID,
    author_id   UUID,
    -- NULL for attachment-only messages
    content     TEXT,
    edited_at   TIMESTAMP,
    -- soft delete: set is_deleted = true and NULL out content on delete
    is_deleted  BOOLEAN,
    -- reply threading: NULL = top-level message
    reply_to_id UUID,
    PRIMARY KEY ((channel_id), created_at, id)
) WITH CLUSTERING ORDER BY (created_at DESC, id DESC)
   AND comment = 'Channel message history, partitioned per channel';

-- ---------------------------------------------------------------------------
-- Direct messages
-- conversation_id is a deterministic UUID derived from sorted(sender_id, recipient_id):
--   UUID5(namespace=DNS, name=min(a,b).str + ":" + max(a,b).str)
-- Both users compute the same conversation_id without coordination.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS direct_messages (
    conversation_id UUID,
    created_at      TIMESTAMP,
    id              UUID,
    sender_id       UUID,
    recipient_id    UUID,
    content         TEXT,
    edited_at       TIMESTAMP,
    is_deleted      BOOLEAN,
    reply_to_id     UUID,
    PRIMARY KEY ((conversation_id), created_at, id)
) WITH CLUSTERING ORDER BY (created_at DESC, id DESC)
   AND comment = 'DM history, partitioned per conversation pair';

-- ---------------------------------------------------------------------------
-- Message attachments (metadata only; actual files stored in object storage)
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS message_attachments (
    channel_id  UUID,
    message_id  UUID,
    id          UUID,
    url         TEXT,
    filename    TEXT,
    size_bytes  BIGINT,
    mime_type   TEXT,
    PRIMARY KEY ((channel_id, message_id), id)
) WITH comment = 'File attachment metadata per channel message';

CREATE TABLE IF NOT EXISTS dm_attachments (
    conversation_id UUID,
    message_id      UUID,
    id              UUID,
    url             TEXT,
    filename        TEXT,
    size_bytes      BIGINT,
    mime_type       TEXT,
    PRIMARY KEY ((conversation_id, message_id), id)
) WITH comment = 'File attachment metadata per DM';

-- ---------------------------------------------------------------------------
-- Message reactions
--
-- Two tables required (CQL forbids mixing COUNTER and non-COUNTER columns):
--   message_reactions       — which users reacted with which emoji (hover list)
--   message_reaction_counts — COUNTER for the displayed total per emoji
--
-- On react:   INSERT into message_reactions + UPDATE reaction_counts count += 1
-- On unreact: DELETE from message_reactions + UPDATE reaction_counts count -= 1
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS message_reactions (
    channel_id  UUID,
    message_id  UUID,
    emoji       TEXT,
    user_id     UUID,
    created_at  TIMESTAMP,
    PRIMARY KEY ((channel_id, message_id), emoji, user_id)
) WITH comment = 'Per-user reaction records; used to check if the current user already reacted';

CREATE TABLE IF NOT EXISTS message_reaction_counts (
    channel_id  UUID,
    message_id  UUID,
    emoji       TEXT,
    count       COUNTER,
    PRIMARY KEY ((channel_id, message_id), emoji)
) WITH comment = 'Reaction totals per emoji; COUNTER for atomic increments';

-- ---------------------------------------------------------------------------
-- Channel read states
-- Tracks how far each user has read in each channel.
-- Used to compute unread counts and "new messages" dividers in the UI.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS channel_read_states (
    user_id              UUID,
    channel_id           UUID,
    last_read_message_id UUID,
    last_read_at         TIMESTAMP,
    PRIMARY KEY ((user_id), channel_id)
) WITH comment = 'Per-user last-read position per channel';

-- ---------------------------------------------------------------------------
-- DM conversation index
-- Lists all DM threads for a user, most recent first.
--
-- partner_id is the clustering key (NOT last_message_at) so that last_message_at
-- can be updated with a regular CQL UPDATE. If last_message_at were a clustering
-- key, updating it would require DELETE + INSERT, leaving ghost rows at the old
-- timestamp. Application layer sorts the result by last_message_at.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS user_conversations (
    user_id          UUID,
    partner_id       UUID,
    conversation_id  UUID,
    last_message_at  TIMESTAMP,
    -- first ~100 chars of the most recent message for the DM sidebar preview
    last_preview     TEXT,
    is_archived      BOOLEAN,
    PRIMARY KEY ((user_id), partner_id)
) WITH comment = 'DM conversation index per user; sort by last_message_at in application layer';

-- DM unread counts — separate COUNTER table (CQL forbids mixing COUNTER and
-- non-COUNTER columns in the same table as user_conversations above)
CREATE TABLE IF NOT EXISTS dm_unread_counts (
    user_id     UUID,
    partner_id  UUID,
    count       COUNTER,
    PRIMARY KEY ((user_id), partner_id)
) WITH comment = 'Unread DM message count per conversation; reset on read';

-- ---------------------------------------------------------------------------
-- Message lookup
-- Supports PATCH /messages/{id} and DELETE /messages/{id}.
-- The REST surface takes only a message UUID; mutating a ScyllaDB row requires
-- the full primary key. This table provides the mapping.
--
-- is_dm routes the mutation to the correct table (messages vs direct_messages).
-- author_id is stored here to authorise edit/delete without a round-trip fetch.
-- Written in the same logged batch as the message INSERT.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS message_lookup (
    message_id       UUID PRIMARY KEY,
    -- routing
    is_dm            BOOLEAN,
    -- for channel messages (is_dm = false)
    channel_id       UUID,
    -- for DMs (is_dm = true)
    conversation_id  UUID,
    -- common
    created_at       TIMESTAMP,
    author_id        UUID
) WITH comment = 'Auxiliary lookup: message_id → full PK + author_id for REST mutations';
