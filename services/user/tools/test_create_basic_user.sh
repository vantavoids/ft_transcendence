#!/usr/bin/env bash
set -euo pipefail

API_BASE_URL="${API_BASE_URL:-https://localhost:1443}"
REGISTER_PATH="${REGISTER_PATH:-/api/auth/v1/register}"
DB_CONTAINER="${DB_CONTAINER:-user-db}"
DB_NAME="${DB_NAME:-user_db}"
DB_USER="${DB_USER:-user}"
EMAIL="${EMAIL:-basic-user-$(date +%s)@example.com}"
PASSWORD="${PASSWORD:-StrongPass123!}"
RETRIES="${RETRIES:-20}"
SLEEP_SECONDS="${SLEEP_SECONDS:-1}"

tmp_response="$(mktemp)"
trap 'rm -f "$tmp_response"' EXIT

echo "Creating user: $EMAIL"

http_code="$(
  curl -sk \
    -o "$tmp_response" \
    -w '%{http_code}' \
    -X POST "${API_BASE_URL}${REGISTER_PATH}" \
    -H 'Content-Type: application/json' \
    -d "{\"email\":\"${EMAIL}\",\"password\":\"${PASSWORD}\"}"
)"

if [[ "$http_code" != "201" ]]; then
  echo "Register request failed with HTTP $http_code"
  cat "$tmp_response"
  exit 1
fi

user_id="$(
  python3 -c 'import json,sys; print(json.load(sys.stdin)["user_id"])' < "$tmp_response"
)"

if [[ ! "$user_id" =~ ^[0-9]+$ ]]; then
  echo "Invalid user_id returned by auth service: $user_id"
  exit 1
fi

echo "Registered user_id: $user_id"
echo "Waiting for user profile creation..."

for attempt in $(seq 1 "$RETRIES"); do
  profile_exists="$(
    docker exec -i "$DB_CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" -tAc \
      "select count(*) from users_profile where id = $user_id;"
  )"

  profile_exists="${profile_exists//[$'\t\r\n ']}"

  if [[ "$profile_exists" == "1" ]]; then
    echo "Profile found in users_profile"
    docker exec -i "$DB_CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" -c \
      "select id, username, status, created_at from users_profile where id = $user_id;"
    exit 0
  fi

  sleep "$SLEEP_SECONDS"
done

echo "Profile was not created after ${RETRIES} attempts"
echo "Last known user_id: $user_id"
exit 1
