package handler

import (
	"net/http"
	"strconv"

	core "github.com/vantavoids/ft_transcendence/services/notification/internal/core"
)

type export struct {
	UserID                  string               `json:"user_id"`
	NotificationPreferences []core.PreferenceDTO `json:"notification_preferences"`
}

func exportHandler(orch *core.Orchestrator) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		exp := export{
			UserID:                  r.PathValue("user_id"),
			NotificationPreferences: []core.PreferenceDTO{},
		}

		uid, err := strconv.ParseInt(r.PathValue("user_id"), 10, 64)
		if err != nil {
			writeJSON(w, http.StatusBadRequest, "invalid user_id")
			return
		}

		rows, err := orch.ListPrefs(r.Context(), uid)
		if err != nil {
			writeJSON(w, http.StatusInternalServerError, "internal error")
			return
		}

		for _, p := range rows {
			exp.NotificationPreferences = append(exp.NotificationPreferences, core.ToPreferenceDTO(p))
		}

		writeJSON(w, http.StatusOK, exp)
	}
}
