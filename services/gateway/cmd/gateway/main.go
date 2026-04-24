package main

import (
	"log"
	"net/http"

	"github.com/vantavoids/ft_transcendence/services/gateway/config"
	"github.com/vantavoids/ft_transcendence/services/gateway/handler"
	"github.com/vantavoids/ft_transcendence/services/gateway/middleware"
	"github.com/vantavoids/ft_transcendence/services/gateway/ratelimit"
)

func main() {

	handler.InitProxies(config.GetServices())

	mux := http.NewServeMux()

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
