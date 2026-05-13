package main

import (
	"log"
	"net/http"
	"time"

	"github.com/vantavoids/ft_transcendence/services/gateway/config"
	"github.com/vantavoids/ft_transcendence/services/gateway/handler"
	"github.com/vantavoids/ft_transcendence/services/gateway/logs"
	"github.com/vantavoids/ft_transcendence/services/gateway/middleware"
	"github.com/vantavoids/ft_transcendence/services/gateway/ratelimit"
)

func main() {

	cfg, err := config.Load()
	if err != nil {
		log.Fatal(err)
	}
	logs.SetDebug(cfg.Debug)

	proxies, err := handler.InitProxies(cfg.Services)
	if err != nil {
		log.Fatal(err)
	}

	mux := http.NewServeMux()

	if cfg.Dev == true {
		mux.HandleFunc("/api/openapi.json", handler.AggregateOpenAPI(cfg))
	}
	mux.HandleFunc("/api/gateway/health", handler.Healthcheck())
	mux.HandleFunc("/api/{rest...}", handler.Redirect(proxies))

	//  ─────────────────────────────────────────────
	//    ╭───────────────────────────────────────╮
	//    │     Timeout Category layer (last)     │
	//    ╰───────────────────────────────────────╯
	timeoutCatHandler := middleware.TimeoutCat(mux)

	//    ╭───────────────────────────────────────╮
	//    │        UID rate limiting layer        │
	//    ╰───────────────────────────────────────╯
	memoryStoreUID := ratelimit.NewMemoryStore(
		time.Duration(cfg.Limits.IdleUID)*time.Minute,
		cfg.Limits.RateUID, cfg.Limits.BucketUID, "UID")

	limitUIDHandler := middleware.LimitUID(memoryStoreUID)(timeoutCatHandler)

	//    ╭───────────────────────────────────────╮
	//    │            JWT auth layer             │
	//    ╰───────────────────────────────────────╯
	jwtAuthHandler := middleware.JwtAuth(cfg.JWTSecret)(limitUIDHandler)

	//    ╭───────────────────────────────────────╮
	//    │        IP rate limiting layer         │
	//    ╰───────────────────────────────────────╯
	memoryStoreIP := ratelimit.NewMemoryStore(
		time.Duration(cfg.Limits.IdleIP)*time.Minute,
		cfg.Limits.RateIP, cfg.Limits.BucketIP, "IP")

	limitIPHandler := middleware.LimitIP(memoryStoreIP)(jwtAuthHandler)

	// special handler to limit request not aimed at backend services
	limitIPHandlerSpecial := middleware.LimitIP(memoryStoreIP)(mux)

	//    ╭───────────────────────────────────────╮
	//    │        Route validation layer         │
	//    ╰───────────────────────────────────────╯
	routeCheckHandler := middleware.RouteCheck(limitIPHandler)

	//    ╭───────────────────────────────────────╮
	//    │    Bypass middleware layer (first)    │
	//    ╰───────────────────────────────────────╯
	dispatchHandler := middleware.Dispatch(limitIPHandlerSpecial, routeCheckHandler)

	//  ─────────────────────────────────────────────

	srv := &http.Server{
		Addr:    ":" + cfg.Port,
		Handler: dispatchHandler,
		// General values used before a timeout category
		// can be attributed to the request in routeCheck()
		ReadHeaderTimeout: 5 * time.Second,   // max time to read request headers from the client
		ReadTimeout:       15 * time.Second,  // max time to read the entire request, headers + body
		WriteTimeout:      20 * time.Second,  // max time to write the response, measured from end of header read
		IdleTimeout:       120 * time.Second, // close idle keep-alive conns after this; falls back to ReadTimeout if zero
	}

	go func() {
		ticker := time.NewTicker(1 * time.Minute)
		defer ticker.Stop()

		for range ticker.C {
			memoryStoreIP.CleanPartial()
			memoryStoreUID.CleanPartial()
		}
	}()

	log.Fatal(srv.ListenAndServe())
}
