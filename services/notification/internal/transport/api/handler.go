package api

import (
	"context"
	"encoding/json"
	"errors"
	"log"
	"net/http"
	"strconv"

	core "github.com/vantavoids/ft_transcendence/services/notification/internal/core"
	"github.com/vantavoids/ft_transcendence/services/notification/internal/platform/failure"
)

type Handler struct {
	svc *core.Orchestrator
}

func NewHandler(svc *core.Orchestrator) (*Handler, error) {
	return &Handler{svc: svc}, nil
}

func (h *Handler) Routes(secret string) http.Handler {
	mux := http.NewServeMux()

	mux.HandleFunc("GET /v1/notifications", h.ListHandler)
	mux.HandleFunc("PATCH /v1/notifications/{id}/read", h.MarkReadHandler)
	mux.HandleFunc("GET /v1/notifications/unread-count", h.UnreadCountHandler)
	mux.HandleFunc("PATCH /v1/notifications/read-all", h.MarkReadAllHandler)
	mux.HandleFunc("DELETE /v1/notifications/{id}", h.DismissHandler)

	mux.HandleFunc("GET /healthz", h.healthzHandler)

	return LoggingMiddleware()(JwtMiddleware(secret)(mux))
}

// GET /notifications
func (h *Handler) ListHandler(w http.ResponseWriter, r *http.Request) {

	userID, ok := getUserIDFromContext(r.Context())
	if !ok {
		writeJSON(w, http.StatusInternalServerError, errorBody("internal error"))
	}

	var read *bool
	readParam := r.URL.Query().Get("read")
	if readParam != "" {
		b, err := strconv.ParseBool(readParam)
		if err != nil {
			writeJSON(w, http.StatusBadRequest, errorBody("invalid read"))
			return
		}
		read = &b
	}

	var includeDismissed *bool
	includeDismissedParam := r.URL.Query().Get("include_dismissed")
	if includeDismissedParam != "" {
		b, err := strconv.ParseBool(includeDismissedParam)
		if err != nil {
			writeJSON(w, http.StatusBadRequest, errorBody("invalid include dismissed"))
			return
		}
		includeDismissed = &b
	}

	var before *int64
	beforeParam := r.URL.Query().Get("before")
	if beforeParam != "" {
		parsed, err := strconv.ParseInt(beforeParam, 10, 64)
		if err != nil {
			writeJSON(w, http.StatusBadRequest, errorBody("invalid before"))
			return
		}
		before = &parsed
	}

	var limit int32 = 50
	limitParam := r.URL.Query().Get("limit")
	if limitParam != "" {
		parsed, err := strconv.ParseInt(limitParam, 10, 32)
		if err != nil {
			writeJSON(w, http.StatusBadRequest, errorBody("invalid limit"))
			return
		}
		limit = int32(min(max(parsed, 1), 100)) // clamp
	}

	notifs, err := h.svc.List(r.Context(), userID, core.ListInput{
		Read:             read,
		IncludeDismissed: includeDismissed,
		Before:           before,
		RowLimit:         limit,
	})
	if err != nil {
		writeError(w, r, err)
		return
	}

	dtos := make([]NotificationDTO, len(notifs))
	for i, n := range notifs {
		dtos[i] = ToDTO(n)
	}
	writeJSON(w, http.StatusOK, dtos)
}

// PATCH /notifications/{id}/read
func (h *Handler) MarkReadHandler(w http.ResponseWriter, r *http.Request) {

	userID, ok := getUserIDFromContext(r.Context())
	if !ok {
		writeJSON(w, http.StatusInternalServerError, errorBody("internal error"))
	}

	id, err := strconv.ParseInt(r.PathValue("id"), 10, 64)
	if err != nil {
		writeJSON(w, http.StatusBadRequest, errorBody("invalid id"))
		return
	}

	err = h.svc.MarkRead(r.Context(), userID, id)
	if err != nil {
		writeError(w, r, err)
		return
	}
	writeJSON(w, http.StatusOK, map[string]any{"id": id, "read": true})
}

// GET /notifications/unread-count
func (h *Handler) UnreadCountHandler(w http.ResponseWriter, r *http.Request) {

	userID, ok := getUserIDFromContext(r.Context())
	if !ok {
		writeJSON(w, http.StatusInternalServerError, errorBody("internal error"))
	}

	rows, err := h.svc.UnreadCount(r.Context(), userID)
	if err != nil {
		writeError(w, r, err)
		return
	}
	writeJSON(w, http.StatusOK, map[string]any{"count": rows})
}

// PATCH /notifications/read-all
func (h *Handler) MarkReadAllHandler(w http.ResponseWriter, r *http.Request) {

	userID, ok := getUserIDFromContext(r.Context())
	if !ok {
		writeJSON(w, http.StatusInternalServerError, errorBody("internal error"))
	}

	rows, err := h.svc.MarkReadAll(r.Context(), userID)
	if err != nil {
		writeError(w, r, err)
		return
	}
	writeJSON(w, http.StatusOK, map[string]any{"updated": rows})
}

// DELETE /notifications/{id}
func (h *Handler) DismissHandler(w http.ResponseWriter, r *http.Request) {
	userID, ok := getUserIDFromContext(r.Context())
	if !ok {
		writeJSON(w, http.StatusInternalServerError, errorBody("internal error"))
	}

	id, err := strconv.ParseInt(r.PathValue("id"), 10, 64)
	if err != nil {
		writeJSON(w, http.StatusBadRequest, errorBody("invalid id"))
		return
	}

	err = h.svc.Dismiss(r.Context(), userID, id)
	if err != nil {
		writeError(w, r, err)
		return
	}
	writeJSON(w, http.StatusNoContent, nil)
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

func errorBody(msg string) map[string]string {
	return map[string]string{"error": msg}
}

func writeError(w http.ResponseWriter, r *http.Request, err error) {
	switch {
	case errors.Is(err, failure.ErrNotFound):
		writeJSON(w, http.StatusNotFound, errorBody("not found"))
	case errors.Is(err, failure.ErrForbidden):
		writeJSON(w, http.StatusForbidden, errorBody("forbidden"))
	default:
		log.Printf("error %s %s: %v", r.Method, r.URL.Path, err)
		writeJSON(w, http.StatusInternalServerError, errorBody("internal error"))
	}
}
