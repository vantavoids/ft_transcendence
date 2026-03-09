# API Contract - Auth Service

Base path: `/auth`

All responses are `application/json`. Errors follow the shape:
```json
{ "error": "human-readable message" }
```

---

## Endpoints

### POST /auth/register

Create a new account.

**Auth required:** No

**Request body:**
```json
{
  "email": "user@example.com",
  "password": "AMuchBetterPasswordThanThisHopefully"
}
```

**Response `201`:**
```json
{
  "access_token": "<jwt>",
  "user_id": "<uuid>"
}
```
Refresh token is set as an `HttpOnly` cookie (`refresh_token`).

**Errors:**
| Status | Reason |
|--------|--------|
| 400 | Invalid email format or weak password |
| 409 | Email already registered |

**Side effects:** Publishes `user.registered { user_id, email }` to RabbitMQ.

---

### POST /auth/login

Login with email and password.

**Auth required:** No

**Request body:**
```json
{
  "email": "user@example.com",
  "password": "AMuchBetterPasswordThanThisHopefully"
}
```

**Response `200`:**
```json
{
  "access_token": "<jwt>",
  "user_id": "<uuid>"
}
```
Refresh token is set/rotated as an `HttpOnly` cookie.

**Errors:**
| Status | Reason |
|--------|--------|
| 400 | Missing fields |
| 401 | Invalid credentials |

---

### POST /auth/refresh

Issue a new access token using the refresh token cookie.

**Auth required:** No (uses `HttpOnly` cookie `refresh_token`)

**Request body:** None

**Response `200`:**
```json
{
  "access_token": "<jwt>"
}
```
Old refresh token is invalidated; new one is set in cookie.

**Errors:**
| Status | Reason |
|--------|--------|
| 401 | Missing, expired, or revoked refresh token |

---

### POST /auth/logout

Revoke the refresh token.

**Auth required:** Yes (`Authorization: Bearer <access_token>`)

**Request body:** None

**Response `204`:** No content.

**Side effects:** Publishes `user.logged_out { user_id }` to RabbitMQ.

**Errors:**
| Status | Reason |
|--------|--------|
| 401 | Invalid or missing access token |

---

### GET /auth/oauth/{provider}

Start an OAuth 2.0 authorization flow. Redirects the client to the provider's consent screen.

**Providers:** `google`, `github`, `intra 42`

**Auth required:** No

**Response `302`:** Redirect to provider consent URL.

**Errors:**
| Status | Reason |
|--------|--------|
| 400 | Unknown provider |

---

### GET /auth/oauth/{provider}/callback

OAuth callback - exchanges the authorization code for tokens, creates or logs in the user.

**Auth required:** No (called by the OAuth provider)

**Query params:**
| Param | Description |
|-------|-------------|
| `code` | Authorization code from provider |
| `state` | CSRF state token |

**Response `302`:** Redirects to frontend with access token as a short-lived query param or sets cookie.

**Side effects:** If new user - inserts row in `users_auth`, publishes `user.registered`.

**Errors:**
| Status | Reason |
|--------|--------|
| 400 | Invalid state / missing code |
| 502 | Provider returned an error |

---

## JWT Claims

Access tokens include:
```json
{
  "sub": "<user_id>",
  "email": "user@example.com",
  "iat": 1710000000,
  "exp": 1710000900
}
```

- Access token TTL: **15 minutes**
- Refresh token TTL: **7 days**

---

## RabbitMQ Events Published

| Event | Payload | Trigger |
|-------|---------|---------|
| `user.registered` | `{ user_id, email }` | Successful register (email or OAuth) |
| `user.logged_out` | `{ user_id }` | Successful logout |

