package main

import (
	"log"
	"net/http"
	"os"
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

	if os.Getenv("DEV") == "true" {
		mux.HandleFunc("/api/openapi.json", handler.AggregateOpenAPI(cfg))
	}

	mux.HandleFunc("/api/{rest...}", handler.Redirect(proxies))

	//  ────────────────────────────────────────────
	//    ╭──────────────────────────────────────╮
	//    │    Timeout Category layer (last)     │
	//    ╰──────────────────────────────────────╯
	timeoutCatHandler := middleware.TimeoutCat(mux)
	//    ╭──────────────────────────────────────╮
	//    │       UID rate limiting layer        │
	//    ╰──────────────────────────────────────╯
	memoryStoreUID := ratelimit.NewMemoryStore(
		cfg.Limits.RateUID, cfg.Limits.BucketUID)

	limitUIDHandler := middleware.LimitUID(memoryStoreUID)(timeoutCatHandler)
	//    ╭──────────────────────────────────────╮
	//    │            JWT auth layer            │
	//    ╰──────────────────────────────────────╯
	jwtAuthHandler := middleware.JwtAuth(cfg.JWTSecret)(limitUIDHandler)
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
		// General values used before a timeout category
		// can be attributed to the request in routeCheck()
		ReadHeaderTimeout: 5 * time.Second,   // max time to read request headers from the client
		ReadTimeout:       15 * time.Second,  // max time to read the entire request, headers + body
		WriteTimeout:      20 * time.Second,  // max time to write the response, measured from end of header read
		IdleTimeout:       120 * time.Second, // close idle keep-alive conns after this; falls back to ReadTimeout if zero
	}

	log.Fatal(srv.ListenAndServe())
}
