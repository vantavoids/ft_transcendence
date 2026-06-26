package middleware

import (
	"context"
	"net/http"
	"strings"

	"github.com/vantavoids/ft_transcendence/services/gateway/logs"
)

type serviceKey struct{}
type timeoutCatKey struct{}

type TimeoutCategory uint8

const (
	CatJSON       TimeoutCategory = 1
	CatUpload     TimeoutCategory = 2
	CatWebSocket  TimeoutCategory = 3
	CatAttachment TimeoutCategory = 4
)

var validServices = map[string]bool{
	"auth":         true,
	"chat":         true,
	"guild":        true,
	"notification": true,
	"user":         true,
}

var wsCapable = map[string]bool{
	"chat": true,
}

func RouteCheck(next http.Handler) http.Handler {

	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {

		if !isAPIRoute(r.URL.Path) {
			http.NotFound(w, r)
			return
		}

		ctx := updateContextServiceAndTimeout(r)

		// log
		host := SourceAddrFromCtx(ctx)
		logs.Info(host, "Route checked, forwarding...")
		next.ServeHTTP(w, r.WithContext(ctx))
	})
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

func updateContextServiceAndTimeout(r *http.Request) context.Context {

	service := fetchService(r.URL.Path)

	ctxService := context.WithValue(r.Context(), serviceKey{}, service)
	ctxTimeoutCat := context.WithValue(ctxService, timeoutCatKey{}, pickTimeoutCat(r, service))

	return ctxTimeoutCat
}

func fetchService(path string) string {

	parts := strings.Split(path, "/")
	return parts[2]
}

func pickTimeoutCat(r *http.Request, service string) TimeoutCategory {

	if isWebSocketUpgrade(r) && wsCapable[service] {
		return CatWebSocket // 3
	}
	if isAttachmentUpload(r) {
		return CatAttachment // 4
	}
	if isAvatarUpload(r) {
		return CatUpload // 2
	}
	return CatJSON // 1
}

// chat file attachments: POST /api/chat/v1/attachments. Capped at 25 MB per file
// by the Chat Service, so this only needs a larger body budget than plain JSON.
func isAttachmentUpload(r *http.Request) bool {

	if r.Method != http.MethodPost {
		return false
	}

	parts := strings.Split(strings.TrimSuffix(r.URL.Path, "/"), "/")
	if len(parts) != 5 {
		return false
	}

	return parts[2] == "chat" && parts[4] == "attachments"
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
