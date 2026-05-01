// Package middleware provides the gateway's HTTP middleware chain:
// route validation, JWT authentication, and rate limiting.
package middleware

import (
	"net/http"
)

type Middleware func(http.Handler) http.Handler
