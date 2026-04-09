// Package JWT middleware provides JWT authentification for the request hitting gateway
package middleware

import (
	"encoding/json"
	"fmt"
	"log"
	"net/http"
	"os"
	"strconv"
	"strings"

	"github.com/golang-jwt/jwt/v5"
	"github.com/vantavoids/ft_transcendence/services/gateway/utils"
)

func checkToken(tokenStr string) error {

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

	if len(parts) < 4 || parts[1] != "api" || parts[3] != "auth" {
		return false
	}

	version := parts[2]
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

		if isAuthRoute(r.URL.Path) {
			next.ServeHTTP(w, r)
			return
		}

		tokenStr := r.Header.Get("Authorization")

		if tokenStr == "" || !strings.HasPrefix(tokenStr, "Bearer ") {
			w.WriteHeader(http.StatusUnauthorized) // 401
			err_msg := "missing authorization header"
			fmt.Fprint(w, err_msg)
			fmt.Println(utils.RedStr(err_msg))
			return
		}

		tokenStr = strings.TrimPrefix(tokenStr, "Bearer ")

		err := checkToken(tokenStr)
		if err != nil {
			w.WriteHeader(http.StatusUnauthorized) // 401
			fmt.Fprint(w, err.Error())
			fmt.Println(utils.RedStr(err.Error()))
			return
		}

		next.ServeHTTP(w, r)
	})
}
