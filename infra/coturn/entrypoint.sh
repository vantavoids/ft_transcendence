#!/bin/sh
# coturn entrypoint: pin the relay / advertised address to a single IPv4.
#
# why: with host networking and no explicit address, coturn auto-discovers
# every interface and round-robins relay allocations across all of them -
# including the docker bridge (172.17.0.1) and a public IPv6. A peer device on
# the LAN cannot reach those, so any call that lands on the wrong candidate
# fails with rb=0 / allocation timeout. Pinning relay-ip + external-ip to one
# reachable IPv4 removes that footgun
#
# address resolution order:
#   1. TURN_RELAY_IP from the environment (explicit override), else
#   2. the source IP of the host's default route (its primary LAN address)
#
# the image's own `detect-external-ip` is deliberately NOT used: it returns the
# public WAN IP via DNS, which is wrong for a LAN-only 1-on-1 relay (the peer is
# on the private subnet, not the internet)
set -eu

RELAY_IP="${TURN_RELAY_IP:-}"
if [ -z "$RELAY_IP" ]; then
  RELAY_IP="$(ip route get 1.1.1.1 2>/dev/null | sed -n 's/.* src \([0-9.]*\).*/\1/p' | head -n1)"
fi

set -- turnserver -c /etc/coturn/turnserver.conf \
  --realm="${TURN_REALM}" \
  --user="${TURN_USERNAME}:${TURN_PASSWORD}"

if [ -n "$RELAY_IP" ]; then
  echo "coturn: pinning relay/external IP to $RELAY_IP"
  set -- "$@" --relay-ip="$RELAY_IP" --external-ip="$RELAY_IP"
else
  echo "coturn: WARNING could not determine a LAN IP; falling back to coturn auto-discovery (relay may pick an unreachable interface, and that'd be unfortunate)" >&2
fi

exec "$@"
