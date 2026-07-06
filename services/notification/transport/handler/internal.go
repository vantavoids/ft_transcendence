package handler

import (
	"net/http"

	"github.com/vantavoids/ft_transcendence/services/notification/internal/core"
)

func exportHandler(orch *core.Orchestrator) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		
	}
}
