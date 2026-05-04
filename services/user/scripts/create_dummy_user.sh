#!/usr/bin/env sh

set -eu

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
SERVICE_DIR=$(CDPATH= cd -- "$SCRIPT_DIR/.." && pwd)
ENV_FILE="$SERVICE_DIR/.env"

if [ -f "$ENV_FILE" ]; then
  # Export DB credentials used by the compose service fallback.
  set -a
  # shellcheck disable=SC1090
  . "$ENV_FILE"
  set +a
fi

USER_ID=${USER_ID:-$(date +%s%N)}
USERNAME=${USERNAME:-dummy_${USER_ID}}
DISPLAY_NAME=${DISPLAY_NAME:-Dummy User}
STATUS=${STATUS:-offline}
BIO=${BIO:-Generated dummy user}
AVATAR_URL=${AVATAR_URL:-}
BANNER_URL=${BANNER_URL:-}
LAST_SEEN_AT=${LAST_SEEN_AT:-}

run_sql() {
  if [ -n "${DATABASE_URL:-}" ] && command -v psql >/dev/null 2>&1; then
    psql "$DATABASE_URL" "$@"
    return
  fi

  podman exec -i user-db \
    psql -U "${POSTGRES_USER:-user}" -d "${POSTGRES_DB:-user}" "$@"
}

run_sql \
  -v ON_ERROR_STOP=1 \
  -v user_id="$USER_ID" \
  -v username="$USERNAME" \
  -v display_name="$DISPLAY_NAME" \
  -v status="$STATUS" \
  -v bio="$BIO" \
  -v avatar_url="$AVATAR_URL" \
  -v banner_url="$BANNER_URL" \
  -v last_seen_at="$LAST_SEEN_AT" \
  <<'SQL'
INSERT INTO users_profile (
    id,
    username,
    display_name,
    avatar_url,
    banner_url,
    status,
    last_seen_at,
    bio
)
VALUES (
    :'user_id'::bigint,
    :'username',
    NULLIF(:'display_name', ''),
    NULLIF(:'avatar_url', ''),
    NULLIF(:'banner_url', ''),
    :'status'::user_status,
    NULLIF(:'last_seen_at', '')::timestamptz,
    NULLIF(:'bio', '')
)
ON CONFLICT (id) DO UPDATE
SET
    username = EXCLUDED.username,
    display_name = EXCLUDED.display_name,
    avatar_url = EXCLUDED.avatar_url,
    banner_url = EXCLUDED.banner_url,
    status = EXCLUDED.status,
    last_seen_at = EXCLUDED.last_seen_at,
    bio = EXCLUDED.bio,
    updated_at = NOW()
RETURNING id, username, display_name, status, created_at, updated_at;
SQL
