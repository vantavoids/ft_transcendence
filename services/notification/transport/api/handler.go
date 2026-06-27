package api

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"log"
	"net/http"
	"strconv"
	"time"

	core "github.com/vantavoids/ft_transcendence/services/notification/internal/core"
	failure "github.com/vantavoids/ft_transcendence/services/notification/internal/platform/failure"
)

type Handler struct {
	svc *core.Orchestrator
	hub *core.Hub
}

func NewHandler(svc *core.Orchestrator, hub *core.Hub) (*Handler, error) {
	return &Handler{svc: svc, hub: hub}, nil
}

func (h *Handler) Routes(secret string) http.Handler {
	mux := http.NewServeMux()

	mux.HandleFunc("GET /notifications", listHandler(h.svc))
	mux.HandleFunc("PATCH /notifications/{id}/read", markReadHandler(h.svc))
	mux.HandleFunc("GET /notifications/unread-count", unreadCountHandler(h.svc))
	mux.HandleFunc("PATCH /notifications/read-all", markReadAllHandler(h.svc))
	mux.HandleFunc("DELETE /notifications/{id}", dismissHandler(h.svc))

	mux.HandleFunc("GET /notifications/events", sseHandler(h.hub))

	return JwtMiddleware(secret)(mux)
}

// GET /notifications
func listHandler(svc *core.Orchestrator) http.HandlerFunc {
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
func markReadHandler(svc *core.Orchestrator) http.HandlerFunc {
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
func unreadCountHandler(svc *core.Orchestrator) http.HandlerFunc {
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
func markReadAllHandler(svc *core.Orchestrator) http.HandlerFunc {
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
func dismissHandler(svc *core.Orchestrator) http.HandlerFunc {
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

func sseHandler(hub *core.Hub) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		userID, ok := getUserIDFromContext(r.Context())
		if !ok {
			writeJSON(w, http.StatusUnauthorized, errorBody("unauthorized error"))
			return
		}

		w.Header().Set("Content-Type", "text/event-stream")
		w.Header().Set("Cache-Control", "no-cache")
		w.Header().Set("X-Accel-Buffering", "no")

		rc := http.NewResponseController(w)
		ch := hub.Subscribe(userID)
		defer hub.Unsubscribe(userID, ch)

		rc.Flush()
		ticker := time.NewTicker(15 * time.Second)
		defer ticker.Stop()

		for {
			select {
			case <-r.Context().Done():
				log.Printf("Client: %d disconnected", userID)
				return
			case notif := <-ch:
				data, err := json.Marshal(notif)
				if err != nil {
					log.Printf("failed to marshal notification: %v", err)
					continue
				}
				fmt.Fprintf(w, "event: ReceiveNotification\n")
				fmt.Fprintf(w, "data: %s\n\n", data)
				if err := rc.Flush(); err != nil {
					return
				}
			case <-ticker.C:
				if _, err := fmt.Fprintf(w, ": ping\n\n"); err != nil {
					return
				}
				if err := rc.Flush(); err != nil {
					return
				}
			}
		}
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
