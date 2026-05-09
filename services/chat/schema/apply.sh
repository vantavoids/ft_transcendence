#!/bin/sh
set -e

until cqlsh chat-db -e 'SHOW VERSION' >/dev/null 2>&1; do
	echo "waiting for chat-db..."
	sleep 2
done

for f in /schema/V*.cql; do
	echo "applying $(basename "$f")"
	cqlsh chat-db -f "$f"
done
