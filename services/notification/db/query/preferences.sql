-- name: UpsertNotificationPreference :one
INSERT INTO notification_preferences (user_id, scope_type, scope_id, muted, muted_until)
VALUES ($1, $2, $3, $4, $5)
ON CONFLICT (user_id, scope_type, scope_id)
DO UPDATE SET
    muted = EXCLUDED.muted,
    muted_until = EXCLUDED.muted_until
RETURNING *;

-- name: ListNotificationPreferences :many
SELECT * FROM notification_preferences
WHERE user_id = $1;

-- name: RemoveNotificationPreference :execrows
DELETE FROM notification_preferences
WHERE user_id = $1 AND scope_type = $2 AND scope_id = $3;

-- name: IsNotificationPreferenceMuted :one
SELECT EXISTS (
    SELECT 1
    FROM notification_preferences
    WHERE user_id = $1
        AND scope_type = $2
        AND scope_id = $3
        AND muted = true
        AND (muted_until IS NULL OR muted_until > NOW())
);