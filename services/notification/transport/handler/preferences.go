package handler

import (
	"net/http"

	"github.com/vantavoids/ft_transcendence/services/notification/internal/core"
)

// GET /notifications/preferences
func listPreferenceHandler(orch *core.Orchestrator) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {

	}
}

// PUT /notifications/preferences/{scope_type}/{scope_id}
func upsertPreferenceHandler(orch *core.Orchestrator) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {

	}
}

// DELETE /notifications/preferences/{scope_type}/{scope_id}
func removePreferenceHandler(orch *core.Orchestrator) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {

	}
}
