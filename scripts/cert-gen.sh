#!/bin/sh
set -e

BLUE="\033[36m"
RESET="\033[0m"

MKCERT_VERSION="v1.4.4"
MKCERT_BIN=".mkcert/binary"
CAROOT=".mkcert"

if [ -f "certs/cert.pem" ] && [ -f "certs/key.pem" ]; then
  printf "Certs already exist, skipping.\n---\n"
  exit 0
fi

mkdir -p "$CAROOT" certs

if [ ! -f "$MKCERT_BIN" ]; then
  curl -fsSL "https://github.com/FiloSottile/mkcert/releases/download/${MKCERT_VERSION}/mkcert-${MKCERT_VERSION}-linux-amd64" \
    -o "$MKCERT_BIN"
  chmod +x "$MKCERT_BIN"
fi

CAROOT="$CAROOT" "$MKCERT_BIN" \
  -cert-file certs/cert.pem \
  -key-file certs/key.pem \
  localhost 127.0.0.1 ::1

rm -f $MKCERT_BIN

printf "- CA certificate is at: ${BLUE}${CAROOT}/rootCA.pem${RESET}\n"
printf "- Import into Firefox:\n"
printf "${BLUE}about:preferences#privacy${RESET} -> ${BLUE}Manage certificates${RESET} -> ${BLUE}Authorities${RESET} -> ${BLUE}Import${RESET}\n---\n"
