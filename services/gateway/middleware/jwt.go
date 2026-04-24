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

<<<<<<< HEAD
	token, err := jwt.Parse(tokenStr, func(token *jwt.Token) (any, error) {

		secret := []byte(os.Getenv("JWT_SECRET"))
		if len(secret) == 0 {
			return secret, fmt.Errorf("missing secret")
		}
		return secret, nil
	})

	if err != nil {
		return err
	}

	if !token.Valid {
		return fmt.Errorf("invalid token")
	}

	// print token for debug
	data, _ := json.MarshalIndent(token, "", "  ")
	fmt.Println("\n" + string(data) + "\n")

	return nil
}

func isAuthRoute(path string) bool {

	parts := strings.Split(path, "/")
	return len(parts) > 2 && parts[2] == "auth"
}

func isAPIRoute(path string) bool {

	parts := strings.Split(path, "/")

	if len(parts) < 4 || parts[0] != "" || parts[1] != "api" {
		return false
	}

	version := parts[3]
	if !strings.HasPrefix(version, "v") {
		return false
	}

	_, err := strconv.Atoi(version[1:])

	return err == nil
}

func JwtAuthMiddleware(next http.Handler) http.Handler {

	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {

		fmt.Println()
		log.Printf("- Request from %s to %s", utils.BlueStr(r.RemoteAddr), utils.BlueStr(r.URL.String()))

		if !isAPIRoute(r.URL.Path) {
			next.ServeHTTP(w, r)
			return
		}

		if isAuthRoute(r.URL.Path) {
=======
func JwtAuth(next http.Handler) http.Handler {

	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {

		isAuth := r.Context().Value(isAuthKey).(bool)
		if isAuth {
			// log
			fmt.Println("JWT auth bypassed, forwarding ...")
>>>>>>> 59a0b85 (feat: add basic rate limiting with IP and UID checks depending on the requested service, add separate routing middleware, start integrating Vanta branch, todo clean stale entries inside the memory store)
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
