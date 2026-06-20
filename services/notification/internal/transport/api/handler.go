package api

import (
	"context"
	"encoding/json"
	"errors"
	"net/http"
	"strconv"

	notification "github.com/vantavoids/ft_transcendence/services/notification/internal/notification"
	"github.com/vantavoids/ft_transcendence/services/notification/internal/platform/failure"
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

	userID, ok := getUserIDFromContext(r.Context())
	if !ok {
		http.Error(w, "internal error", http.StatusInternalServerError)
		return
	}

	id, err := strconv.ParseInt(r.PathValue("id"), 10, 64)
	if err != nil {
		http.Error(w, "invalid id", http.StatusBadRequest)
		return
	}

	err = h.svc.MarkRead(r.Context(), userID, id)

	switch {
	case errors.Is(err, failure.ErrNotFound):
		http.Error(w, "not found", http.StatusNotFound)
	case errors.Is(err, failure.ErrForbidden):
		http.Error(w, "forbidden", http.StatusForbidden)
	case err != nil:
		http.Error(w, "internal error", http.StatusInternalServerError)
	default:
		writeJSON(w, http.StatusOK, map[string]any{"id": id, "read": true})
	}
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

func writeJSON(w http.ResponseWriter, status int, body any) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	if body != nil {
		json.NewEncoder(w).Encode(body)
	}
}
