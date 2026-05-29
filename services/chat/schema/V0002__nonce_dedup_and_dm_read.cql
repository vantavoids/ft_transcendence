USE chat;

CREATE TABLE IF NOT EXISTS message_nonce_dedup (
    author_id   BIGINT,
    channel_id  BIGINT,
    nonce       TEXT,
    message_id  BIGINT,
    PRIMARY KEY ((author_id, channel_id), nonce)
) WITH default_time_to_live = 600
   AND comment = 'Idempotency dedup for POST /channels/{id}/messages; auto-expires after the 10-minute window';

CREATE TABLE IF NOT EXISTS dm_nonce_dedup (
    sender_id     BIGINT,
    recipient_id  BIGINT,
    nonce         TEXT,
    message_id    BIGINT,
    PRIMARY KEY ((sender_id, recipient_id), nonce)
) WITH default_time_to_live = 600
   AND comment = 'Idempotency dedup for POST /dms/{user_id}/messages; auto-expires after the 10-minute window';

CREATE TABLE IF NOT EXISTS dm_read_states (
    user_id              BIGINT,
    partner_id           BIGINT,
    last_read_message_id BIGINT,
    last_read_at         TIMESTAMP,
    PRIMARY KEY ((user_id), partner_id)
) WITH comment = 'Per-user last-read position per DM partner';
