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

// TODO: err is lost this way if we dont return a better error log

// GET /notifications
func (h *Handler) ListHandler(w http.ResponseWriter, r *http.Request) {

	userID, ok := getUserIDFromContext(r.Context())
	if !ok {
		http.Error(w, "internal error", http.StatusInternalServerError)
		return
	}

	var read *bool
	readParam := r.URL.Query().Get("read")
	switch readParam {
	case "false":
		*read = false
	case "true":
		*read = true
	default:
		http.Error(w, "invalid read", http.StatusBadRequest)
		return
	}

	var includeDismissed *bool
	includeDismissedParam := r.URL.Query().Get("include_dismissed")
	switch includeDismissedParam {
	case "false":
		*includeDismissed = false
	case "true":
		*includeDismissed = true
	default:
		http.Error(w, "invalid include dismissed", http.StatusBadRequest)
		return
	}

	var before *int64
	beforeParam := r.URL.Query().Get("before")
	if beforeParam != "" {
		parsed, err := strconv.ParseInt(beforeParam, 10, 64)
		if err != nil {
			http.Error(w, "invalid before", http.StatusBadRequest)
			return
		}
		before = &parsed
	}

	var limit int32 = 50
	limitParam := r.URL.Query().Get("limit")
	if limitParam != "" {
		parsed, err := strconv.ParseInt(limitParam, 10, 32)
		if err != nil {
			http.Error(w, "invalid limit", http.StatusBadRequest)
			return
		}
		limit = int32(min(max(parsed, 0), 100)) // clamp
	}

	notifs, err := h.svc.List(r.Context(), userID, notification.ListInput{
		Read:             read,
		IncludeDismissed: includeDismissed,
		Before:           before,
		RowLimit:         limit,
	})
	if err != nil {
		http.Error(w, "internal error", http.StatusInternalServerError)
		return
	}

	dtos := make([]NotificationDTO, len(notifs))
	for i, n := range notifs {
		dtos[i] = ToDTO(n)
	}	
	writeJSON(w, 200, dtos)
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

	userID, ok := getUserIDFromContext(r.Context())
	if !ok {
		http.Error(w, "internal error", http.StatusInternalServerError)
		return
	}

	rows, err := h.svc.UnreadCount(r.Context(), userID)
	switch {
	case err != nil:
		http.Error(w, "internal error", http.StatusInternalServerError)
	default:
		writeJSON(w, http.StatusOK, map[string]any{"count": rows})
	}
}

// PATCH /notifications/read-all
func (h *Handler) MarkReadAllHandler(w http.ResponseWriter, r *http.Request) {

	userID, ok := getUserIDFromContext(r.Context())
	if !ok {
		http.Error(w, "internal error", http.StatusInternalServerError)
		return
	}

	rows, err := h.svc.MarkReadAll(r.Context(), userID)
	switch {
	case err != nil:
		http.Error(w, "internal error", http.StatusInternalServerError)
	default:
		writeJSON(w, http.StatusOK, map[string]any{"updated": rows})
	}
}

// DELETE /notifications/{id}
func (h *Handler) DismissHandler(w http.ResponseWriter, r *http.Request) {
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

	err = h.svc.Dismiss(r.Context(), userID, id)
	switch {
	case errors.Is(err, failure.ErrNotFound):
		http.Error(w, "not found", http.StatusNotFound)
	case errors.Is(err, failure.ErrForbidden):
		http.Error(w, "forbidden", http.StatusForbidden)
	case err != nil:
		http.Error(w, "internal error", http.StatusInternalServerError)
	default:
		writeJSON(w, http.StatusNoContent, nil)
	}
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
