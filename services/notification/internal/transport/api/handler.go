package api

import (
	"context"
	"net/http"

	notification "github.com/vantavoids/ft_transcendence/services/notification/internal/notification"
)

type Handler struct {
	svc *notification.Service
}

func NewHandler(svc *notification.Service) (*Handler, error) {
	return &Handler{svc: svc}, nil
}

func (h *Handler) Routes(secret string) http.Handler {
	mux := http.NewServeMux()

	mux.HandleFunc("GET /notifications", h.ListHandler)
	mux.HandleFunc("PATCH /notifications/{id}/read", h.MarkReadHandler)
	mux.HandleFunc("GET /notifications/unread-count", h.UnreadCountHandler)
	mux.HandleFunc("PATCH /notifications/read-all", h.MarkReadAllHandler)
	mux.HandleFunc("DELETE /notifications/{id}", h.DismissHandler)

	return JwtMiddleware(secret)(mux)
}

// GET /notifications
func (h *Handler) ListHandler(w http.ResponseWriter, r *http.Request) {

}

// PATCH /notifications/{id}/read
func (h *Handler) MarkReadHandler(w http.ResponseWriter, r *http.Request) {

}

// GET /notifications/unread-count
func (h *Handler) UnreadCountHandler(w http.ResponseWriter, r *http.Request) {

}

// PATCH /notifications/read-all
func (h *Handler) MarkReadAllHandler(w http.ResponseWriter, r *http.Request) {

}

// DELETE /notifications/{id}
func (h *Handler) DismissHandler(w http.ResponseWriter, r *http.Request) {

}

func getUserIDFromContext(ctx context.Context) (int64, bool) {
	userID, ok := ctx.Value(userIDKey{}).(int64)
	return userID, ok
}
