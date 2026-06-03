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
USERNAME="${USERNAME:-dummy-$(date +%s)}"
DISPLAY_NAME="${DISPLAY_NAME:-Dummy User}"
STATUS="${STATUS:-offline}"
BIO="${BIO:-}"

if [[ ${#USERNAME} -gt 32 ]]; then
  echo "USERNAME is too long for users_profile.username (max 32 chars)"
  exit 1
fi

if [[ ${#DISPLAY_NAME} -gt 64 ]]; then
  echo "DISPLAY_NAME is too long for users_profile.display_name (max 64 chars)"
  exit 1
fi

sql_escape() {
  local value="$1"
  printf "%s" "${value//\'/\'\'}"
}

user_id="$(
  python3 -c 'import time; print(time.time_ns())'
)"

username_sql="$(sql_escape "$USERNAME")"
display_name_sql="$(sql_escape "$DISPLAY_NAME")"
status_sql="$(sql_escape "$STATUS")"
bio_sql="NULL"
if [[ -n "$BIO" ]]; then
  bio_sql="'$(sql_escape "$BIO")'"
fi

echo "Creating dummy user:"
echo "  id=$user_id"
echo "  username=$USERNAME"
echo "  display_name=$DISPLAY_NAME"
echo "  status=$STATUS"

$RUNTIME exec -i "$DB_CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" -v ON_ERROR_STOP=1 <<SQL
INSERT INTO users_profile (
  id,
  username,
  display_name,
  bio,
  status
) VALUES (
  ${user_id},
  '${username_sql}',
  '${display_name_sql}',
  ${bio_sql},
  '${status_sql}'::user_status
);

SELECT
  id,
  username,
  display_name,
  status,
  created_at
FROM users_profile
WHERE id = ${user_id};
SQL
