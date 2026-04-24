// Package middleware provides JWT authentification for the request hitting gateway
package middleware

import (
	"context"
	"encoding/json"
	"fmt"
	"net/http"
	"os"
	"strings"

	"github.com/golang-jwt/jwt/v5"
	"github.com/vantavoids/ft_transcendence/services/gateway/utils"
)

const subKey contextKey = "sub"

func JwtAuth(next http.Handler) http.Handler {

	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {

		isAuth := r.Context().Value(isAuthKey).(bool)
		if isAuth {
			// log
			fmt.Println("JWT auth bypassed, forwarding ...")
			next.ServeHTTP(w, r)
			return
		}

		tokenStr := r.Header.Get("Authorization")

		if !strings.HasPrefix(tokenStr, "Bearer ") {
			errMsg := "missing authorization header"
			http.Error(w, errMsg, http.StatusUnauthorized)
			fmt.Println(utils.RedStr(errMsg))
			return
		}

		tokenStr = strings.TrimPrefix(tokenStr, "Bearer ")

		sub, err := checkToken(tokenStr)
		if err != nil {
			errMsg := err.Error()
			http.Error(w, errMsg, http.StatusUnauthorized)
			fmt.Println(utils.RedStr(errMsg))
			return
		}

		ctx := context.WithValue(r.Context(), subKey, sub)

		// log
		fmt.Println("JWT auth passed, forwarding ...")
		next.ServeHTTP(w, r.WithContext(ctx))
	})
}

func checkToken(tokenStr string) (string, error) {

	token, err := jwt.Parse(tokenStr, func(token *jwt.Token) (any, error) {

		secret := []byte(os.Getenv("JWT_SECRET"))
		if len(secret) == 0 {
			return secret, fmt.Errorf("missing secret")
		}
		return secret, nil
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
	data, _ := json.MarshalIndent(token, "", "  ")
	fmt.Println("\n" + string(data) + "\n")

	return sub, nil
}
