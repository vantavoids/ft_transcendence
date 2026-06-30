-- name: ListNotificationPreferences :many
SELECT * FROM notification_preferences
WHERE user_id = $1;

-- name: RemoveNotificationPreference :execrows
DELETE FROM notification_preferences
WHERE user_id = $1 AND scope_type = $2 AND scope_id = $3;