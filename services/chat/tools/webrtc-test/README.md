# WebRTC signaling test harness

A single-page manual harness for exercising the Chat service's SignalR signaling
hub (`/api/chat/v1/hubs/signaling`) end to end: register a throwaway user,
connect, and place a real 1-on-1 WebRTC call between two browsers.

This lives on the `tmp/webrtc-test-harness` branch only. It is test tooling, not
part of the product, and is intentionally kept off `main` and out of the #39 PR.

## Run

With the stack up (either `docker compose up` or `tilt up`), nginx serves the
page from a read-only mount, so no separate static server is needed:

```
https://<host>:1443/webrtc-test.html
```

Open it on two devices on the same network (accept the self-signed cert once per
device). On each: **Register new user** then **Connect**. Copy one side's `me:`
id into the other's `call` box and press **Call**.

Camera and mic need a secure context, which the HTTPS origin provides.

## ICE / TURN

STUN points at the page host on `:3478` by default. TURN is off unless you pass
credentials at runtime (they must never be committed):

```
https://<host>:1443/webrtc-test.html?turnUser=<user>&turnPass=<pass>
```

Optional `?iceHost=<host>` overrides the STUN/TURN host if coturn runs elsewhere.

Same-machine and same-LAN calls usually connect on STUN alone; TURN is only
needed when both peers are behind restrictive NAT.
