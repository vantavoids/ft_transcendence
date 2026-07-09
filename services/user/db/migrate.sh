#!/bin/sh
# Applies the User service schema (db/init.sql) once. init.sql is plain DDL (no
# IF NOT EXISTS), so guard on the presence of users_profile to stay idempotent
# across repeated `make _migrate` runs.
set -e

: "${POSTGRES_HOST:?POSTGRES_HOST not set}"
: "${POSTGRES_PORT:?POSTGRES_PORT not set}"
export PGPASSWORD="$POSTGRES_PASSWORD"

for i in $(seq 1 30); do
	if pg_isready -h "$POSTGRES_HOST" -p "$POSTGRES_PORT" -U "$POSTGRES_USER" -d "$POSTGRES_DB" >/dev/null 2>&1; then
		break
	fi
	echo "waiting for $POSTGRES_HOST:$POSTGRES_PORT... ($i/30)"
	sleep 1
done

exists=$(psql -h "$POSTGRES_HOST" -p "$POSTGRES_PORT" -U "$POSTGRES_USER" -d "$POSTGRES_DB" -tAc "SELECT to_regclass('public.users_profile')")
if [ "$exists" = "users_profile" ]; then
	echo "user schema already present, nothing to do"
else
	echo "applying user schema from init.sql..."
	psql -h "$POSTGRES_HOST" -p "$POSTGRES_PORT" -U "$POSTGRES_USER" -d "$POSTGRES_DB" -v ON_ERROR_STOP=1 -f /migrate/init.sql
	echo "user schema applied"
fi
