CREATE KEYSPACE IF NOT EXISTS chat
    WITH replication = { 'class': 'SimpleStrategy', 'replication_factor': 1 };

CREATE TABLE IF NOT EXISTS chat.messages (
    channel_id uuid,
    bucket     int,
    sent_at    timeuuid,
    author_id  uuid,
    content    text,
    PRIMARY KEY ((channel_id, bucket), sent_at)
) WITH CLUSTERING ORDER BY (sent_at DESC);
