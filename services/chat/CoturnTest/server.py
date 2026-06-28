#!/usr/bin/env python3
"""
Tiny zero-dependency test harness for the coturn (#210) STUN/TURN server.

It does two things:
  1. serves index.html (the WebRTC relay test page) over plain HTTP
  2. acts as a dumb signaling relay so two browsers (on two machines) can
     exchange SDP offers/answers and ICE candidates

TURN credentials are read from the repo root .env at request time and handed to
the page via /config.json, so nothing secret is baked into the committed HTML.

This is a manual test tool, not part of the app. Run it on the coturn host:

    python3 tools/coturn-test/server.py

Then open http://<host>:8088 in a browser. Camera/mic are NOT used (the page
generates synthetic audio+video), so plain HTTP is fine -- no HTTPS needed.
"""

import json
import os
import ssl
import threading
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import urlparse, parse_qs

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT_ENV = os.path.normpath(os.path.join(HERE, "..", "..", ".env"))
CERTS = os.path.normpath(os.path.join(HERE, "..", "..", "certs"))
PORT = int(os.environ.get("PORT", "8088"))
TLS_PORT = int(os.environ.get("TLS_PORT", "8443"))

# room -> {"messages": [ {"id", "from", "msg"} ], "next": int}
_rooms = {}
_lock = threading.Lock()


def read_turn_config():
    """Pull TURN_* out of the root .env (best effort)."""
    cfg = {"username": "ft_turn", "credential": "ft_turn", "realm": "localhost"}
    try:
        with open(ROOT_ENV, "r") as f:
            for line in f:
                line = line.strip()
                if not line or line.startswith("#") or "=" not in line:
                    continue
                key, _, val = line.partition("=")
                key, val = key.strip(), val.strip().strip('"').strip("'")
                if key == "TURN_USERNAME":
                    cfg["username"] = val
                elif key == "TURN_PASSWORD":
                    cfg["credential"] = val
                elif key == "TURN_REALM":
                    cfg["realm"] = val
    except OSError:
        pass
    return cfg


class Handler(BaseHTTPRequestHandler):
    protocol_version = "HTTP/1.1"

    def log_message(self, *args):  # quieter console
        pass

    def _send(self, code, body=b"", ctype="application/json"):
        if isinstance(body, str):
            body = body.encode()
        self.send_response(code)
        self.send_header("Content-Type", ctype)
        self.send_header("Content-Length", str(len(body)))
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Access-Control-Allow-Headers", "Content-Type")
        self.send_header("Access-Control-Allow-Methods", "GET, POST, OPTIONS")
        self.end_headers()
        if body:
            self.wfile.write(body)

    def do_OPTIONS(self):
        self._send(204)

    def do_GET(self):
        parsed = urlparse(self.path)
        path = parsed.path

        if path in ("/", "/index.html"):
            try:
                with open(os.path.join(HERE, "index.html"), "rb") as f:
                    self._send(200, f.read(), "text/html; charset=utf-8")
            except OSError:
                self._send(404, b"index.html missing")
            return

        if path == "/config.json":
            self._send(200, json.dumps(read_turn_config()))
            return

        if path.startswith("/signal/"):
            room = path[len("/signal/"):]
            q = parse_qs(parsed.query)
            since = int(q.get("since", ["0"])[0])
            me = q.get("me", [""])[0]
            with _lock:
                r = _rooms.get(room, {"messages": [], "next": 0})
                out = [m for m in r["messages"] if m["id"] > since and m["from"] != me]
                mx = r["next"]
            self._send(200, json.dumps({"messages": out, "maxId": mx}))
            return

        self._send(404, b"not found")

    def do_POST(self):
        parsed = urlparse(self.path)
        path = parsed.path

        if path.startswith("/signal/"):
            room = path[len("/signal/"):]
            length = int(self.headers.get("Content-Length", "0"))
            try:
                payload = json.loads(self.rfile.read(length) or b"{}")
            except json.JSONDecodeError:
                self._send(400, b'{"error":"bad json"}')
                return
            with _lock:
                r = _rooms.setdefault(room, {"messages": [], "next": 0})
                r["next"] += 1
                r["messages"].append(
                    {"id": r["next"], "from": payload.get("from", "?"), "msg": payload.get("msg")}
                )
                # keep rooms from growing unbounded across reuse
                if len(r["messages"]) > 500:
                    r["messages"] = r["messages"][-500:]
                mx = r["next"]
            self._send(200, json.dumps({"ok": True, "maxId": mx}))
            return

        if path == "/reset":
            with _lock:
                _rooms.clear()
            self._send(200, b'{"ok":true}')
            return

        self._send(404, b"not found")


def serve(server, label):
    print(label)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass


def main():
    cfg = read_turn_config()
    print(f"coturn test harness — TURN user '{cfg['username']}' realm '{cfg['realm']}'")
    print(f"  (creds from {ROOT_ENV}); page auto-uses turn:<hostname>:3478")

    http_srv = ThreadingHTTPServer(("0.0.0.0", PORT), Handler)
    threads = [threading.Thread(
        target=serve, args=(http_srv, f"  HTTP  : http://0.0.0.0:{PORT}  (synthetic media only)"),
        daemon=True)]

    cert, key = os.path.join(CERTS, "cert.pem"), os.path.join(CERTS, "key.pem")
    if os.path.exists(cert) and os.path.exists(key):
        ctx = ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER)
        ctx.load_cert_chain(cert, key)
        https_srv = ThreadingHTTPServer(("0.0.0.0", TLS_PORT), Handler)
        https_srv.socket = ctx.wrap_socket(https_srv.socket, server_side=True)
        threads.append(threading.Thread(
            target=serve, args=(https_srv,
                f"  HTTPS : https://0.0.0.0:{TLS_PORT} (real camera/mic; self-signed cert -> accept the warning)"),
            daemon=True))
    else:
        print(f"  (no certs at {CERTS}; HTTPS disabled, so real camera/mic won't be available)")

    for t in threads:
        t.start()
    try:
        for t in threads:
            t.join()
    except KeyboardInterrupt:
        pass


if __name__ == "__main__":
    main()
