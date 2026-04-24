package main

import (
	"log"
	"net/http"
	"time"

	"github.com/vantavoids/ft_transcendence/services/gateway/config"
	"github.com/vantavoids/ft_transcendence/services/gateway/handler"
	"github.com/vantavoids/ft_transcendence/services/gateway/middleware"
	"github.com/vantavoids/ft_transcendence/services/gateway/ratelimit"
)

func main() {

	cfg, err := config.Load()
	if err != nil {
		log.Fatal(err)
	}

	proxies, err := handler.InitProxies(cfg.Services)
	if err != nil {
		log.Fatal(err)
	}

	mux := http.NewServeMux()

	mux.HandleFunc("/api/{rest...}", handler.Redirect(proxies))

	//  ────────────────────────────────────────────

	// TODO update timeouts based on category

	//    ╭───────────────────────────────╮
	//    │    UID rate limiting layer    │
	//    ╰───────────────────────────────╯
	memoryStoreUID := ratelimit.NewMemoryStore(
		cfg.Limits.RateUID, cfg.Limits.BucketUID)
	limitUIDHandler := middleware.LimitUID(memoryStoreUID)(mux)
	//    ╭──────────────────────────────────────╮
	//    │            JWT auth layer            │
	//    ╰──────────────────────────────────────╯
	jwtAuthHandler := middleware.JwtAuth(limitUIDHandler)
	//    ╭──────────────────────────────────────╮
	//    │        IP rate limiting layer        │
	//    ╰──────────────────────────────────────╯
	memoryStoreIP := ratelimit.NewMemoryStore(
		cfg.Limits.RateIP, cfg.Limits.BucketIP)
	limitIPHandler := middleware.LimitIP(memoryStoreIP)(jwtAuthHandler)
	//    ╭──────────────────────────────────────╮
	//    │    Route validation layer (first)    │
	//    ╰──────────────────────────────────────╯
	routeCheckHandler := middleware.RouteCheck(limitIPHandler)
	//  ────────────────────────────────────────────

	srv := &http.Server{
		Addr:    ":" + cfg.Port,
		Handler: routeCheckHandler,

		ReadHeaderTimeout: 5 * time.Second,
		ReadTimeout:       15 * time.Second,
		WriteTimeout:      15 * time.Second,
		IdleTimeout:       120 * time.Second,
	}

	log.Fatal(srv.ListenAndServe())
}
