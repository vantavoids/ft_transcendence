package middleware

import (
	"context"
	"encoding/json"
	"fmt"
	"net/http"
	"strings"

	"github.com/golang-jwt/jwt/v5"
	"github.com/vantavoids/ft_transcendence/services/gateway/utils"
)

type subKey struct{}

func JwtAuth(secret string) Middleware {

	return func(next http.Handler) http.Handler {

		return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {

			serviceName, ok := r.Context().Value(serviceKey{}).(string)
			if !ok {
				logMsg := "missing serviceKey in ctx inside JwtAuth"
				errMsg := "internal server error"
				http.Error(w, errMsg, http.StatusInternalServerError)
				fmt.Println(utils.RedStr(logMsg))
				return
			}

			if serviceName == "auth" {
				// log
				fmt.Println("JWT auth bypassed, forwarding ...")
				next.ServeHTTP(w, r)
				return
			}

			tokenStr := extractToken(r)
			if tokenStr == "" {
				errMsg := "missing authorization header"
				http.Error(w, errMsg, http.StatusUnauthorized)
				fmt.Println(utils.RedStr(errMsg))
				return
			}

			subValue, err := checkToken(tokenStr, secret)
			if err != nil {
				errMsg := err.Error()
				http.Error(w, errMsg, http.StatusUnauthorized)
				fmt.Println(utils.RedStr(errMsg))
				return
			}

			ctx := context.WithValue(r.Context(), subKey{}, subValue)

			// log
			fmt.Println("JWT auth passed, forwarding ...")
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
	if r.Header.Get("Upgrade") == "websocket" {
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
		fmt.Println(utils.RedStr("invalid token"))
		return "", fmt.Errorf("unauthorized")
	}

	claims, ok := token.Claims.(jwt.MapClaims)
	if !ok {
		fmt.Println(utils.RedStr("invalid claims"))
		return "", fmt.Errorf("unauthorized")
	}
	sub, ok := claims["sub"].(string)
	if !ok {
		fmt.Println(utils.RedStr("missing sub claim"))
		return "", fmt.Errorf("unauthorized")
	}

	// print token for debug, TODO remove
	data, _ := json.MarshalIndent(token, "", "  ")
	fmt.Println("\n" + string(data) + "\n")

	return sub, nil
}
