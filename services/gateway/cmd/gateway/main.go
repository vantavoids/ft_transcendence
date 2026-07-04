package main

import (
	"log"
	"net"
	"net/http"
	"os"
	"path/filepath"
	"time"

	"github.com/vantavoids/ft_transcendence/services/gateway/config"
	"github.com/vantavoids/ft_transcendence/services/gateway/handler"
	"github.com/vantavoids/ft_transcendence/services/gateway/logs"
	"github.com/vantavoids/ft_transcendence/services/gateway/middleware"
	"github.com/vantavoids/ft_transcendence/services/gateway/observability"
	"github.com/vantavoids/ft_transcendence/services/gateway/ratelimit"
	"go.opentelemetry.io/contrib/instrumentation/net/http/otelhttp"
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

	if cfg.Dev {
		mux.HandleFunc("/api/openapi.json", handler.AggregateOpenAPI(cfg))
	}
	mux.HandleFunc("/healthz", handler.Healthcheck())
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

	// metrics: otelhttp wraps the whole chain (outermost) so rate-limit and auth
	// rejections are counted too; /metrics is served anonymously above it.
	metricsHandler, err := observability.Setup(map[string]*ratelimit.MemoryStore{
		"ip":  memoryStoreIP,
		"uid": memoryStoreUID,
	})
	if err != nil {
		log.Fatal(err)
	}

	// metrics/healthz over TCP for Prometheus, bridge-only (not host-reachable).
	metricsMux := http.NewServeMux()
	metricsMux.Handle("GET /metrics", metricsHandler)
	metricsMux.HandleFunc("/healthz", handler.Healthcheck())
	go func() {
		log.Printf("metrics + healthz on :%s (TCP)", cfg.MetricsPort)
		log.Fatal(http.ListenAndServe(":"+cfg.MetricsPort, metricsMux))
	}()

	root := http.NewServeMux()
	root.Handle("/", otelhttp.NewHandler(dispatchHandler, "gateway"))

	srv := &http.Server{
		Handler: root,
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

	// unix socket: no TCP port, only nginx (shared volume) can connect.
	socketPath := cfg.SocketPath
	if err := os.MkdirAll(filepath.Dir(socketPath), 0755); err != nil {
		log.Fatal(err)
	}
	os.Remove(socketPath) // clean up stale socket from a previous run
	listener, err := net.Listen("unix", socketPath)
	if err != nil {
		log.Fatal(err)
	}
	if err := os.Chmod(socketPath, 0666); err != nil {
		log.Fatal(err)
	}

	log.Printf("gateway listening on unix socket %s", socketPath)
	log.Fatal(srv.Serve(listener))
}

