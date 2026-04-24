## Fixes

- IP rate limiting currently keys on r.RemoteAddr. In the docker-compose setup the gateway is behind nginx, so RemoteAddr will be the nginx container IP for all clients, effectively rate-limiting the proxy instead of individual users. Consider extracting the client IP from X-Real-IP / X-Forwarded-For (with appropriate trust rules) and fall back to RemoteAddr when those headers are absent.

- fix commits artifacts
