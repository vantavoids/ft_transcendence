package handler

import (
	"net/http"

	core "github.com/vantavoids/ft_transcendence/services/notification/internal/core"
)

type Handler struct {
	orch *core.Orchestrator
	hub  *core.Hub
}

func NewHandler(svc *core.Orchestrator, hub *core.Hub) (*Handler, error) {
	return &Handler{orch: svc, hub: hub}, nil
}

func (h *Handler) Routes(secret string) http.Handler {
	mux := http.NewServeMux()

	mux.HandleFunc("GET /notifications", listNotificationHandler(h.orch))
	mux.HandleFunc("PATCH /notifications/{id}/read", markReadNotificationHandler(h.orch))
	mux.HandleFunc("GET /notifications/unread-count", unreadCountNotificationHandler(h.orch))
	mux.HandleFunc("PATCH /notifications/read-all", markReadAllNotificationHandler(h.orch))
	mux.HandleFunc("DELETE /notifications/{id}", dismissNotificationHandler(h.orch))

	mux.HandleFunc("GET /notifications/preferences", listPreferenceHandler(h.orch))
	mux.HandleFunc("PUT /notifications/preferences/{scope_type}/{scope_id}", upsertPreferenceHandler(h.orch))
	mux.HandleFunc("DELETE /notifications/preferences/{scope_type}/{scope_id}", removePreferenceHandler(h.orch))

	mux.HandleFunc("GET /notifications/events", sseHandler(h.hub))

	mux.HandleFunc("GET /heatlz", healthzHandler)

	return LoggingMiddleware()(JwtMiddleware(secret)(mux))
}
