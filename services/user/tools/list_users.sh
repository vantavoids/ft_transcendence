#!/usr/bin/env bash
set -euo pipefail

RUNTIME="${RUNTIME:-}"
if [[ -z "$RUNTIME" ]]; then
  if command -v podman >/dev/null 2>&1; then
    RUNTIME="podman"
  else
    RUNTIME="docker"
  fi
fi

DB_CONTAINER="${DB_CONTAINER:-user-db}"
DB_NAME="${DB_NAME:-user_db}"
DB_USER="${DB_USER:-user}"
LIMIT="${LIMIT:-50}"

if ! [[ "$LIMIT" =~ ^[0-9]+$ ]] || [[ "$LIMIT" -lt 1 ]]; then
  echo "LIMIT must be a positive integer"
  exit 1
fi

$RUNTIME exec -i "$DB_CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" -v ON_ERROR_STOP=1 -c \
  "SELECT id, username, COALESCE(display_name, '') AS display_name, status, created_at
   FROM users_profile
   ORDER BY created_at DESC
   LIMIT ${LIMIT};"
