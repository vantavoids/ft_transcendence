package handler

import (
	"encoding/json"
	"net/http"
	"strconv"
	"time"

	"github.com/vantavoids/ft_transcendence/services/notification/internal/core"
)

type Payload struct {
	Muted      bool       `json:"muted"`
	MutedUntil *time.Time `json:"muted_until"`
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
		if err := json.NewDecoder(r.Body).Decode(&body); err != nil {
			writeJSON(w, http.StatusBadRequest, errorBody("invalid request body"))
			return
		}

		if body.MutedUntil != nil {
			if !body.Muted {
				writeJSON(w, http.StatusBadRequest, errorBody("invalid muted_until requires muted=true"))
				return
			}
			if body.MutedUntil.Before(time.Now()) {
				writeJSON(w, http.StatusBadRequest, errorBody("invalid muted_until must be in the future"))
				return
			}
		}

		pref, err := orch.UpsertPrefs(r.Context(), core.UpsertInput{
			UserID:     userID,
			ScopeType:  scopeType,
			ScopeID:    scopeID,
			Muted:      body.Muted,
			MutedUntil: body.MutedUntil,
		})
		if err != nil {
			writeError(w, r, err)
			return
		}

		writeJSON(w, http.StatusOK, core.ToPreferenceDTO(pref))
	}
}

// GET /notifications/preferences
func listPreferenceHandler(orch *core.Orchestrator) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		userID, ok := getUserIDFromContext(r.Context())
		if !ok {
			writeJSON(w, http.StatusUnauthorized, errorBody("unauthorized invalid"))
			return
		}

		rows, err := orch.ListPrefs(r.Context(), userID)
		if err != nil {
			writeError(w, r, err)
			return
		}

		dtos := make([]core.PreferenceDTO, len(rows))
		for i, n := range rows {
			dtos[i] = core.ToPreferenceDTO(n)
		}

		writeJSON(w, http.StatusOK, dtos)
	}
}

// DELETE /notifications/preferences/{scope_type}/{scope_id}
func removePreferenceHandler(orch *core.Orchestrator) http.HandlerFunc {
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

		rows, err := orch.RemovePrefs(r.Context(), userID, scopeType, scopeID)
		if err != nil {
			writeError(w, r, err)
			return
		}

		if rows == 0 {
			writeJSON(w, http.StatusNotFound, errorBody("no preference set for this scope"))
			return
		}

		writeJSON(w, http.StatusNoContent, nil)
	}
}
