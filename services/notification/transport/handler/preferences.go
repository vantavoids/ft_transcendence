package handler

import (
	"encoding/json"
	"net/http"
	"strconv"
	"time"

	"github.com/vantavoids/ft_transcendence/services/notification/internal/core"
)

type Payload struct {
	Muted      bool      `json:"muted"`
	MutedUntil time.Time `json:"muted_until"`
}

// PUT /notifications/preferences/{scope_type}/{scope_id}
func upsertPreferenceHandler(orch *core.Orchestrator) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		userID, ok := getUserIDFromContext(r.Context())
		if !ok {
			writeJSON(w, http.StatusUnauthorized, errorBody("unauthorized invalid"))
			return
		}

		scopeType := r.PathValue("scope_type")
		if scopeType != "guild" && scopeType != "channel" {
			writeJSON(w, http.StatusBadRequest, errorBody("invalid scope type"))
			return
		}

		scopeID, err := strconv.ParseInt(r.PathValue("scope_id"), 10, 64)
		if err != nil {
			writeJSON(w, http.StatusBadRequest, errorBody("invalid scope id"))
			return
		}

		var body Payload
		err = json.NewDecoder(r.Body).Decode(&body)
		if err != nil {
			writeJSON(w, http.StatusBadRequest, errorBody("invalid request body"))
			return
		}

		orch.UpsertPrefs(r.Context(), core.UpsertInput{
			UserID:     userID,
			ScopeType:  scopeType,
			ScopeID:    scopeID,
			Muted:      body.Muted,
			MutedUntil: body.MutedUntil,
		})
	}
}

// GET /notifications/preferences
func listPreferenceHandler(orch *core.Orchestrator) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {

	}
}

// DELETE /notifications/preferences/{scope_type}/{scope_id}
func removePreferenceHandler(orch *core.Orchestrator) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {

	}
}
