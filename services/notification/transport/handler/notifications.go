package handler

import (
	"context"
	"encoding/json"
	"errors"
	"log"
	"net/http"
	"strconv"

	core "github.com/vantavoids/ft_transcendence/services/notification/internal/core"
	failure "github.com/vantavoids/ft_transcendence/services/notification/internal/platform/failure"
	"go.opentelemetry.io/contrib/instrumentation/net/http/otelhttp"
)

// GET /notifications
func listNotificationHandler(svc *core.Orchestrator) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		userID, ok := getUserIDFromContext(r.Context())
		if !ok {
			writeJSON(w, http.StatusUnauthorized, errorBody("unauthorized invalid"))
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

		notifs, err := svc.List(r.Context(), userID, core.ListInput{
			Read:             read,
			IncludeDismissed: includeDismissed,
			Before:           before,
			RowLimit:         limit,
		})
		if err != nil {
			writeError(w, r, err)
			return
		}

		dtos := make([]core.NotificationREST, len(notifs))
		for i, n := range notifs {
			dtos[i] = core.ToREST(n)
		}
		writeJSON(w, http.StatusOK, dtos)
	}
}

// PATCH /notifications/{id}/read
func markReadNotificationHandler(svc *core.Orchestrator) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		userID, ok := getUserIDFromContext(r.Context())
		if !ok {
			writeJSON(w, http.StatusUnauthorized, errorBody("unauthorized invalid"))
			return
		}

		id, err := strconv.ParseInt(r.PathValue("id"), 10, 64)
		if err != nil {
			writeJSON(w, http.StatusBadRequest, errorBody("invalid id"))
			return
		}

		err = svc.MarkRead(r.Context(), userID, id)
		if err != nil {
			writeError(w, r, err)
			return
		}
		writeJSON(w, http.StatusOK, map[string]any{"id": id, "read": true})
	}
}

// GET /notifications/unread-count
func unreadCountNotificationHandler(svc *core.Orchestrator) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		userID, ok := getUserIDFromContext(r.Context())
		if !ok {
			writeJSON(w, http.StatusUnauthorized, errorBody("unauthorized invalid"))
			return
		}

		rows, err := svc.UnreadCount(r.Context(), userID)
		if err != nil {
			writeError(w, r, err)
			return
		}
		writeJSON(w, http.StatusOK, map[string]any{"count": rows})
	}
}

// PATCH /notifications/read-all
func markReadAllNotificationHandler(svc *core.Orchestrator) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		userID, ok := getUserIDFromContext(r.Context())
		if !ok {
			writeJSON(w, http.StatusUnauthorized, errorBody("unauthorized invalid"))
			return
		}

		rows, err := svc.MarkReadAll(r.Context(), userID)
		if err != nil {
			writeError(w, r, err)
			return
		}
		writeJSON(w, http.StatusOK, map[string]any{"updated": rows})
	}
}

// DELETE /notifications/{id}
func dismissNotificationHandler(svc *core.Orchestrator) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		userID, ok := getUserIDFromContext(r.Context())
		if !ok {
			writeJSON(w, http.StatusUnauthorized, errorBody("unauthorized invalid"))
			return
		}

		id, err := strconv.ParseInt(r.PathValue("id"), 10, 64)
		if err != nil {
			writeJSON(w, http.StatusBadRequest, errorBody("invalid id"))
			return
		}

		err = svc.Dismiss(r.Context(), userID, id)
		if err != nil {
			writeError(w, r, err)
			return
		}
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
