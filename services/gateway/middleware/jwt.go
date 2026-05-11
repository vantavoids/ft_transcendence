package middleware

import (
	"context"
	"fmt"
	"net/http"
	"strings"

	"github.com/golang-jwt/jwt/v5"
	"github.com/vantavoids/ft_transcendence/services/gateway/logs"
)

type subKey struct{}

func JwtAuth(secret string) Middleware {

	return func(next http.Handler) http.Handler {

		return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {

			host := r.Context().Value(SourceAddrKey{}).(string)

			serviceName, ok := r.Context().Value(serviceKey{}).(string)
			if !ok {
				http.Error(w, "internal server error", http.StatusInternalServerError)
				logs.Error(host, "missing serviceKey in ctx inside JwtAuth")
				return
			}

			if serviceName == "auth" {
				// log
				logs.Info(host, "JWT auth bypassed, forwarding ...")
				next.ServeHTTP(w, r)
				return
			}

			tokenStr := extractToken(r)
			if tokenStr == "" {
				errMsg := "missing authorization header"
				http.Error(w, errMsg, http.StatusUnauthorized)
				logs.Error(host, errMsg)

				return
			}

			subValue, err := checkToken(tokenStr, secret)
			if err != nil {
				errMsg := err.Error()
				http.Error(w, "unauthorized", http.StatusUnauthorized)
				logs.Error(host, errMsg)
				return
			}

			ctx := context.WithValue(r.Context(), subKey{}, subValue)

			// log
			logs.Info(host, "JWT auth passed, forwarding ...")
			next.ServeHTTP(w, r.WithContext(ctx))
		})
	}
}

func extractToken(r *http.Request) string {

	if h := r.Header.Get("Authorization"); h != "" {
		if scheme, token, ok := strings.Cut(h, " "); ok && strings.EqualFold(scheme, "bearer") {
			return strings.TrimSpace(token) // TrimSpace just to be safe if extra space is present
		}
	}
	if isWebSocketUpgrade(r) {
		return r.URL.Query().Get("access_token")
	}

	return ""
}

func checkToken(tokenStr string, secret string) (string, error) {

	// check the token alg to be HMAC and return secret
	token, err := jwt.Parse(tokenStr, func(token *jwt.Token) (any, error) {
		if _, ok := token.Method.(*jwt.SigningMethodHMAC); !ok {
			return nil, fmt.Errorf("unexpected signing method: %v", token.Header["alg"])
		}
		return []byte(secret), nil
	})

	if err != nil {
		return "", err
	}

	if !token.Valid {
		return "", fmt.Errorf("invalid token")
	}

	claims, ok := token.Claims.(jwt.MapClaims)
	if !ok {
		return "", fmt.Errorf("invalid claims")
	}
	sub, ok := claims["sub"].(string)
	if !ok {
		return "", fmt.Errorf("missing sub claim")
	}

	// print token for debug, TODO remove
	// data, _ := json.MarshalIndent(token, "", "  ")
	// fmt.Println("\n" + string(data) + "\n")

	return sub, nil
}
