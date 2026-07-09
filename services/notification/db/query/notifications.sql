-- name: CreateNotification :one
INSERT INTO notifications (id, user_id, type, actor_id, source_id, payload)
VALUES ($1, $2, $3, $4, $5, $6)
RETURNING *;

-- name: GetNotificationByID :one
SELECT * FROM notifications
WHERE id = $1;

-- name: MarkNotificationRead :execrows
UPDATE notifications
SET read_at = NOW()
WHERE id = $1 AND user_id = $2;

-- name: MarkAllNotificationsRead :execrows
UPDATE notifications
SET read_at = NOW()
WHERE user_id = $1 AND read_at IS NULL;

-- name: CountUnreadNotifications :one
SELECT COUNT(*)
FROM notifications
WHERE user_id = $1 AND read_at IS NULL AND dismissed_at IS NULL;

-- name: DismissNotification :execrows
-- dismissing implies the user saw it: mark unread rows read in the same write
UPDATE notifications
SET dismissed_at = NOW(),
    read_at = COALESCE(read_at, NOW())
WHERE id = $1 AND user_id = $2;

-- name: GetNotifications :many
SELECT * FROM notifications
WHERE user_id = $1
    AND (sqlc.narg(read)::bool IS NULL OR sqlc.narg(read)::bool = (read_at IS NOT NULL))
    AND (sqlc.narg(include_dismissed)::bool OR dismissed_at IS NULL)
    AND (sqlc.narg(before)::bigint IS NULL OR id < sqlc.narg(before)::bigint )
ORDER BY id DESC
LIMIT sqlc.arg(row_limit)::int;

-- name: DeleteUserNotifications :exec
DELETE FROM notifications
WHERE user_id = $1;

-- name: DeleteNotificationsOlderThan7Days :exec
DELETE FROM notifications
WHERE created_at < NOW() - INTERVAL '7 days';