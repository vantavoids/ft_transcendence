package main

import (
	"log"
	"net/http"

	"github.com/vantavoids/ft_transcendence/services/gateway/handler"
	"github.com/vantavoids/ft_transcendence/services/gateway/middleware"
)

func main() {

	mux := http.NewServeMux()
	mux.HandleFunc("/api/{rest...}", handler.Redirect)

	// mux.HandleFunc("/headers", handler.Headers)

	wrapped := middleware.JwtAuthMiddleware(mux)

	log.Fatal(http.ListenAndServe(":8090", wrapped))
}
