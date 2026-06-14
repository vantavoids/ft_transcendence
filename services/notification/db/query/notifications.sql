-- name: CreateNotification :one
INSERT INTO notifications (id, user_id, type, actor_id, source_id, payload)
VALUES ($1, $2, $3, $4, $5, $6)
RETURNING id, user_id, type, actor_id, source_id, payload, read_at, dismissed_at, created_at;