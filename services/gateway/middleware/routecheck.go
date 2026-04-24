package middleware

import (
	"context"
	"fmt"
	"log"
	"net/http"
	"strconv"
	"strings"

	"github.com/vantavoids/ft_transcendence/services/gateway/utils"
)

type contextKey string

const isAuthKey contextKey = "isAuth"

func RouteCheck(next http.Handler) http.Handler {

	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {

		// log
		fmt.Println()
		log.Printf("- Request from %s to %s", utils.BlueStr(r.RemoteAddr), utils.BlueStr(r.URL.String()))

		if !isAPIRoute(r.URL.Path) {
			http.NotFound(w, r)
			return
		}

		ctx := context.WithValue(r.Context(), isAuthKey, isAuthRoute(r.URL.Path))

		// log
		fmt.Println("Route checked, forwarding...")
		next.ServeHTTP(w, r.WithContext(ctx))
	})
}

var validServices = map[string]bool{
	"auth":          true,
	"chat":          true,
	"guild":         true,
	"notifications": true,
	"user":          true,
}

func isAPIRoute(path string) bool {

	parts := strings.Split(path, "/")

	if len(parts) < 4 || parts[0] != "" || parts[1] != "api" {
		return false
	}

	if !validServices[parts[2]] {
		return false
	}

	version := parts[3]
	if !strings.HasPrefix(version, "v") {
		return false
	}

	_, err := strconv.Atoi(version[1:])

	return err == nil
}

func isAuthRoute(path string) bool {

	parts := strings.Split(path, "/")
	return len(parts) > 2 && parts[2] == "auth"
}
