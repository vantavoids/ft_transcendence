#!/bin/env sh
# generate the self-signed TLS cert used by nginx, host-side
#
# why host-side: it avoids a one-shot cert-gen container (podman dislikes
# `service_completed_successfully` dependencies) and the files end up owned by
# the current user, so the nginx bind mount reads them without permission games
#
# AND once a certificate has been trusted, we don't need to trust it again upon
# a stack restart :D
set -e

BLUE="\033[36m"
RESET="\033[0m"

CERT_DIR="certs"

if [ -f "$CERT_DIR/cert.pem" ] && [ -f "$CERT_DIR/key.pem" ]; then
	printf "Certs already exist, nothing to do\n"
	exit 0
fi

if ! command -v openssl >/dev/null 2>&1; then
	printf "Error: openssl is not installed or not on PATH\n" >&2
	printf "Install it and re-run" >&2
	exit 1
fi

mkdir -p "$CERT_DIR"
openssl req -x509 -newkey rsa:4096 -nodes \
	-keyout "$CERT_DIR/key.pem" -out "$CERT_DIR/cert.pem" \
	-days 365 -subj '/CN=localhost' \
	-addext 'subjectAltName=DNS:localhost,IP:127.0.0.1'

printf "TLS certs generated in ${BLUE}%s/${RESET}\n" "$CERT_DIR"
