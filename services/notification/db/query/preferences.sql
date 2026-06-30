-- name: ListNotificationPreferences :many
SELECT * FROM notification_preferences
WHERE user_id = $1;