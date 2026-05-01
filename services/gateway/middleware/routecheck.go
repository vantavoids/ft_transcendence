package middleware

import (
	"context"
	"fmt"
	"log"
	"net/http"
	"strings"

	"github.com/vantavoids/ft_transcendence/services/gateway/utils"
)

type serviceKey struct{}
type timeoutCatKey struct{}

type TimeoutCategory uint8

const (
	CatJSON      TimeoutCategory = 1
	CatUpload    TimeoutCategory = 2
	CatWebSocket TimeoutCategory = 3
)

var validServices = map[string]bool{
	"auth":          true,
	"chat":          true,
	"guild":         true,
	"notifications": true,
	"user":          true,
}

var wsCapable = map[string]bool{
	"chat": true,
}

func RouteCheck(next http.Handler) http.Handler {

	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {

		// log
		fmt.Println()
		log.Printf("- Request from %s to %s", utils.BlueStr(r.RemoteAddr), utils.BlueStr(r.URL.String()))

		if !isAPIRoute(r.URL.Path) {
			http.NotFound(w, r)
			return
		}

		ctx := updateContext(r)

		// log
		fmt.Println("Route checked, forwarding...")
		next.ServeHTTP(w, r.WithContext(ctx))
	})
}

func isAPIRoute(path string) bool {

	parts := strings.Split(path, "/")

	// special case for WS upgrades
	if len(parts) == 3 && parts[0] == "" && parts[1] == "hubs" && parts[2] == "chat" {
		return true
	}

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

	return isOnlyDigits(version[1:])
}

func isOnlyDigits(s string) bool {

	if s == "" {
		return false
	}

	for _, r := range s {
		if r < '0' || r > '9' {
			return false
		}
	}

	return true
}

func updateContext(r *http.Request) context.Context {

	service := fetchService(r.URL.Path)

	ctxService := context.WithValue(r.Context(), serviceKey{}, service)
	ctxTimeoutCat := context.WithValue(ctxService, timeoutCatKey{}, pickTimeoutCat(r))

	return ctxTimeoutCat
}

func fetchService(path string) string {

	parts := strings.Split(path, "/")
	return parts[2]
}

func pickTimeoutCat(r *http.Request) TimeoutCategory {

	if isWebSocketUpgrade(r) && wsCapable[fetchService(r.URL.Path)] {
		return CatWebSocket // 3
	}
	if isAvatarUpload(r) {
		return CatUpload // 2
	}
	return CatJSON // 1
}

func isWebSocketUpgrade(r *http.Request) bool {

	return r.Method == http.MethodGet &&
		strings.EqualFold(r.Header.Get("Upgrade"), "websocket") &&
		strings.Contains(strings.ToLower(r.Header.Get("Connection")), "upgrade")
}

func isAvatarUpload(r *http.Request) bool {

	if r.Method != http.MethodPost && r.Method != http.MethodPut {
		return false
	}

	parts := strings.Split(strings.TrimSuffix(r.URL.Path, "/"), "/")
	if len(parts) < 6 {
		return false
	}

	service := parts[2]
	if service != "user" && service != "guild" {
		return false
	}

	return isOnlyDigits(parts[4]) && parts[5] == "avatar"
}
