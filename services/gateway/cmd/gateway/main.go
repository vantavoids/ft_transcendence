package main

import (
	"log"
	"net/http"
	"os"

	"github.com/vantavoids/ft_transcendence/services/gateway/config"
	"github.com/vantavoids/ft_transcendence/services/gateway/handler"
	"github.com/vantavoids/ft_transcendence/services/gateway/middleware"
	"github.com/vantavoids/ft_transcendence/services/gateway/ratelimit"
)

func main() {

	handler.InitProxies(config.GetServices())

	mux := http.NewServeMux()
<<<<<<< HEAD
	if os.Getenv("DEV") == "true" {
		mux.HandleFunc("/api/openapi.json", handler.AggregateOpenAPI)
	}
=======

>>>>>>> 59a0b85 (feat: add basic rate limiting with IP and UID checks depending on the requested service, add separate routing middleware, start integrating Vanta branch, todo clean stale entries inside the memory store)
	mux.HandleFunc("/api/{rest...}", handler.Redirect)

	// UID rate limiting layer (last)
	UIDmemoryStore := ratelimit.NewMemoryStore(1, 10)
	UIDLimit := middleware.UIDLimitFunc(UIDmemoryStore)

	UIDLimitWrap := UIDLimit(mux)

	// JWT auth layer
	jwtAuthWrap := middleware.JwtAuth(UIDLimitWrap)

	// IP rate limiting layer
	IPmemoryStore := ratelimit.NewMemoryStore(0.2, 3)
	IPLimit := middleware.IPLimitFunc(IPmemoryStore)

	IPLimitWrap := IPLimit(jwtAuthWrap)

	// Route validation layer (first)
	routeCheckWrap := middleware.RouteCheck(IPLimitWrap)

	log.Fatal(http.ListenAndServe(":8080", routeCheckWrap))
}
