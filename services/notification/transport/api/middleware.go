package api

import (
	"context"
	"fmt"
	"log"
	"net/http"
	"strconv"
	"strings"

	jwt "github.com/golang-jwt/jwt/v5"
)

type userIDKey struct{}

type Middleware func(http.Handler) http.Handler

func LoggingMiddleware() Middleware {
	return func(next http.Handler) http.Handler {
		return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			log.Printf("→ %s %s", r.Method, r.URL.Path)
			next.ServeHTTP(w, r)
		})
	}
}

func JwtMiddleware(secret string) Middleware {
	return func(next http.Handler) http.Handler {
		return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {

			token := extract(r)
			if token == "" {
				http.Error(w, "missing authorization header", http.StatusUnauthorized)
				return
			}

			userID, err := check(token, secret)
			if err != nil {
				http.Error(w, "unauthorized", http.StatusUnauthorized)
				return
			}

			ctx := context.WithValue(r.Context(), userIDKey{}, userID)

			next.ServeHTTP(w, r.WithContext(ctx))
		})
	}
}

func extract(r *http.Request) string {

	if h := r.Header.Get("Authorization"); h != "" {
		if scheme, token, ok := strings.Cut(h, " "); ok && strings.EqualFold(scheme, "bearer") {
			return strings.TrimSpace(token)
		}
	}
	
	return r.URL.Query().Get("access_token")
}

func check(jwtToken string, secret string) (int64, error) {

	token, err := jwt.Parse(jwtToken, func(token *jwt.Token) (any, error) {
		if _, ok := token.Method.(*jwt.SigningMethodHMAC); !ok {
			return nil, fmt.Errorf("unexpected signing method")
		}
		return []byte(secret), nil
	})
	if err != nil {
		return 0, fmt.Errorf("invalid token: %w", err)
	}

	if !token.Valid {
		return 0, fmt.Errorf("invalid token")
	}

	sub, err := token.Claims.GetSubject()
	if err != nil {
		return 0, fmt.Errorf("missing sub claim: %w", err)
	}

	userID, err := strconv.ParseInt(sub, 10, 64)
	if err != nil {
		return 0, fmt.Errorf("invalid sub format: %w", err)
	}

	return userID, nil
}
