package middleware

import (
	"context"
	"net/http"
	"slices"

	"github.com/vantavoids/ft_transcendence/services/gateway/logs"
)

type RateLimitStore interface {
	Allow(identity string) bool
}

func LimitIP(store RateLimitStore) Middleware {

	return func(next http.Handler) http.Handler {

		return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {

			host := r.Context().Value(SourceAddrKey{}).(string)

			svc := serviceFromCtx(r.Context())
			if svc == "" {
				http.Error(w, "internal server error", http.StatusInternalServerError)
				logs.Error(host, "LimitIP: missing service from context")
				return
			}

			if svc != "auth" && svc != "special" {
				// log
				logs.Info(host, "IP limit bypassed, forwarding...")
				next.ServeHTTP(w, r)
				return
			}

			if !isLocalhost(host) {
				if !store.Allow(host) {
					errMsg := "too many requests"
					http.Error(w, errMsg, http.StatusTooManyRequests)
					logs.Error(host, errMsg)
					return
				}
				logs.Info(host, "IP limit passed, forwarding...")
			} else {
				logs.Info(host, "IP limit bypassed by localhost, forwarding...")
			}

			// log
			next.ServeHTTP(w, r)
		})
	}
}

func LimitUID(store RateLimitStore) Middleware {

	return func(next http.Handler) http.Handler {

		return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {

			host := r.Context().Value(SourceAddrKey{}).(string)

			svc := serviceFromCtx(r.Context())
			if svc == "" {
				http.Error(w, "internal server error", http.StatusInternalServerError)
				logs.Error(host, "LimitUID: missing service from context")
				return
			}

			if svc == "auth" {
				// log
				logs.Info(host, "UID limit bypassed, forwarding...")
				next.ServeHTTP(w, r)
				return
			}

			uid := r.Context().Value(subKey{}).(string)

			if !store.Allow(uid) {
				errMsg := "too many requests"
				http.Error(w, errMsg, http.StatusTooManyRequests)
				logs.Error(host, errMsg)
				return
			}

			// log
			logs.Info(host, "UID limit passed, forwarding...")
			next.ServeHTTP(w, r)
		})
	}
}

func serviceFromCtx(ctx context.Context) string {

	s, _ := ctx.Value(serviceKey{}).(string)
	return s
}

func isLocalhost(host string) bool {

	valid := []string{
		"localhost",
		"127.0.0.1",
		"::1",
	}

	return slices.Contains(valid, host)
}
