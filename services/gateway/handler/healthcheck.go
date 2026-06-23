package handler

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

// healthcheck reports the gateway's liveness. the gateway holds no backing
// store of its own, so the only registered check is its own liveness;
// downstream service health is surfaced by each service's own /healthz.
func Healthcheck() http.HandlerFunc {

	return func(w http.ResponseWriter, r *http.Request) {

		w.Header().Set("Content-Type", "application/json")
		w.WriteHeader(http.StatusOK)
		json.NewEncoder(w).Encode(healthReport{
			Status: "Healthy",
			Checks: []healthCheck{
				{Name: "self", Status: "Healthy"},
			},
		})
	}
}
