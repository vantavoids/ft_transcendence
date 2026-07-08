#!/bin/sh
# start the MinIO server, then create the required buckets once it is reachable.
# Folding bucket creation into the server container (instead of a separate
# one-shot) avoids the podman-compose `service_completed_successfully` ->
# `--requires` deadlock: an exited one-shot can't satisfy a podman `--requires`.
# (because podman genuinely sucks ass holy hell)
set -e

minio server /data --console-address ":9001" &
pid=$!

until mc alias set local "http://127.0.0.1:9000" "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD" >/dev/null 2>&1; do
	sleep 1
done

mc mb -p "local/$MINIO_BUCKET" "local/$MINIO_USER_BUCKET" "local/$MINIO_GUILD_BUCKET"
mc anonymous set download "local/$MINIO_USER_BUCKET"
echo "minio: buckets ready ($MINIO_BUCKET, $MINIO_USER_BUCKET, $MINIO_GUILD_BUCKET)"

wait "$pid"
