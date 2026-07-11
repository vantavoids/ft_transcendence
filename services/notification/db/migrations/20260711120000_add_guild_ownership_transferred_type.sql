-- +goose NO TRANSACTION
-- +goose Up
ALTER TYPE notification_type ADD VALUE IF NOT EXISTS 'guild_ownership_transferred';

-- +goose Down
-- Postgres cannot remove a value from an enum type; the value is harmless if unused.
SELECT 1;
