package handler

import (
	"fmt"
	"net/http"

	"github.com/vantavoids/ft_transcendence/services/gateway/healthcheck"
)

type StatusCode = healthcheck.StatusCode

func Healthcheck() http.HandlerFunc {

	return func(w http.ResponseWriter, r *http.Request) {

		fmt.Fprintf(w, "status: OK")
	}
}
