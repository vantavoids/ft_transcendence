package main

import (
	"log"
	"net/http"
	"os"

	"github.com/vantavoids/ft_transcendence/services/gateway/config"
	"github.com/vantavoids/ft_transcendence/services/gateway/handler"
	"github.com/vantavoids/ft_transcendence/services/gateway/middleware"
)

func main() {

	handler.InitProxies(config.GetServices())

	mux := http.NewServeMux()
	if os.Getenv("DEV") == "true" {
		mux.HandleFunc("/api/openapi.json", handler.AggregateOpenAPI)
	}
	mux.HandleFunc("/api/{rest...}", handler.Redirect)

	wrapped := middleware.JwtAuthMiddleware(mux)

	log.Fatal(http.ListenAndServe(":8080", wrapped))
}
