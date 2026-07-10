package handler

import (
	"net/http"

	core "github.com/vantavoids/ft_transcendence/services/notification/internal/core"
	"go.opentelemetry.io/contrib/instrumentation/net/http/otelhttp"
)

type Handler struct {
	orch *core.Orchestrator
	hub  *core.Hub
}

func NewHandler(svc *core.Orchestrator, hub *core.Hub) (*Handler, error) {
	return &Handler{orch: svc, hub: hub}, nil
}

func (h *Handler) Routes(secret string, metricsHandler http.Handler) http.Handler {
	mux := http.NewServeMux()

	// the gateway strips /api/{service} and forwards /v1/..., so every
	// public route carries the version prefix (like the other services)
	mux.HandleFunc("GET /v1/notifications", listNotificationHandler(h.orch))
	mux.HandleFunc("PATCH /v1/notifications/{id}/read", markReadNotificationHandler(h.orch))
	mux.HandleFunc("GET /v1/notifications/unread-count", unreadCountNotificationHandler(h.orch))
	mux.HandleFunc("PATCH /v1/notifications/read-all", markReadAllNotificationHandler(h.orch))
	mux.HandleFunc("DELETE /v1/notifications/{id}", dismissNotificationHandler(h.orch))

	mux.HandleFunc("GET /v1/notifications/preferences", listPreferenceHandler(h.orch))
	mux.HandleFunc("PUT /v1/notifications/preferences/{scope_type}/{scope_id}", upsertPreferenceHandler(h.orch))
	mux.HandleFunc("DELETE /v1/notifications/preferences/{scope_type}/{scope_id}", removePreferenceHandler(h.orch))

	mux.HandleFunc("GET /v1/notifications/events", sseHandler(h.hub))

	// instrument the app mux for the http.server RED metric. otelhttp sits just
	// outside the mux (inside the auth/logging middleware) so it can read the
	// matched route from r.Pattern for the http_route label.
	authed := LoggingMiddleware()(JwtMiddleware(secret)(otelhttp.NewHandler(mux, "notification")))

	// healthz + metrics are anonymous: probed/scraped over the docker network,
	// never through the gateway (which only forwards /api/{service}/vN/...).
	root := http.NewServeMux()
	root.HandleFunc("GET /internal/users/{user_id}/data-export", exportHandler(h.orch))
	root.HandleFunc("GET /healthz", healthzHandler)
	root.Handle("GET /metrics", metricsHandler)
	root.Handle("/", authed)

	return root
}
