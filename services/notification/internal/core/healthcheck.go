package api

import (
	"encoding/json"
	"net/http"
)

type healthCheck struct {
	Name        string  `json:"name"`
	Status      string  `json:"status"`
	Description *string `json:"description"`
}

type healthReport struct {
	Status string        `json:"status"`
	Checks []healthCheck `json:"checks"`
}

// healthz reports the service's liveness. notification has no
// app-layer DB client wired yet, so the only registered check is its own
// liveness; the backing DB is gated separately by the compose healthcheck.
func (h *Handler) healthzHandler(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(http.StatusOK)
	json.NewEncoder(w).Encode(healthReport{
		Status: "Healthy",
		Checks: []healthCheck{
			{Name: "self", Status: "Healthy"},
		},
	})
}
