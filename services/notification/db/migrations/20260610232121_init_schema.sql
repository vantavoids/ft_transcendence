-- +goose Up

CREATE TYPE notification_type AS ENUM (
    'mention',
    'dm',
    'friend_request',
    'guild_invite',
    'guild_welcome',
    'incoming_call'
);

CREATE TABLE notifications (
    id            BIGINT            PRIMARY KEY,
    user_id       BIGINT            NOT NULL,
    type          notification_type NOT NULL,
    actor_id      BIGINT,
    source_id     BIGINT,
    payload       JSONB             NOT NULL DEFAULT '{}',
    read_at       TIMESTAMPTZ,
    dismissed_at  TIMESTAMPTZ,
    created_at    TIMESTAMPTZ       NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_notifications_user_unread ON notifications (user_id, created_at DESC)
    WHERE read_at IS NULL AND dismissed_at IS NULL;

CREATE INDEX idx_notifications_user_all  ON notifications (user_id, created_at DESC);

CREATE INDEX idx_notifications_user_type ON notifications (user_id, type, created_at DESC);

CREATE INDEX idx_notifications_actor     ON notifications (actor_id);

CREATE INDEX idx_notifications_unread ON notifications (user_id)
    WHERE read_at IS NULL AND dismissed_at IS NULL;

CREATE TABLE notification_preferences (
    user_id     BIGINT     NOT NULL,
    scope_type  VARCHAR(8) NOT NULL CHECK (scope_type IN ('guild', 'channel')),
    scope_id    BIGINT     NOT NULL,
    muted       BOOLEAN    NOT NULL DEFAULT FALSE,
    muted_until TIMESTAMPTZ,
    PRIMARY KEY (user_id, scope_type, scope_id)
);

CREATE INDEX idx_notif_prefs_scope ON notification_preferences (scope_type, scope_id);

-- +goose Down
DROP TABLE IF EXISTS notification_preferences;
DROP TABLE IF EXISTS notifications;
DROP TYPE IF EXISTS notification_type;
