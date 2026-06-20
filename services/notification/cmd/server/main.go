package main

import (
	"context"
	"encoding/json"
	"log"
	"net/http"
	"os"
	"time"

	pgxpool "github.com/jackc/pgx/v5/pgxpool"
	database "github.com/vantavoids/ft_transcendence/services/notification/db/sqlc"
	core "github.com/vantavoids/ft_transcendence/services/notification/internal/core"
	snowflake "github.com/vantavoids/ft_transcendence/services/notification/internal/platform/snowflake"
	api "github.com/vantavoids/ft_transcendence/services/notification/internal/transport/api"
	broker "github.com/vantavoids/ft_transcendence/services/notification/internal/transport/broker"
	tunnel "github.com/vantavoids/ft_transcendence/services/notification/internal/tunnel"
)

type healthCheck struct {
	Name        string  `json:"name"`
	Status      string  `json:"status"`
	Description *string `json:"description"`
}

type healthReport struct {
	Status string        `json:"status"`
	Checks []healthCheck `json:"checks"`
}

// healthz reports the service's liveness. notification has no
// app-layer DB client wired yet, so the only registered check is its own
// liveness; the backing DB is gated separately by the compose healthcheck.
func healthz(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(http.StatusOK)
	json.NewEncoder(w).Encode(healthReport{
		Status: "Healthy",
		Checks: []healthCheck{
			{Name: "self", Status: "Healthy"},
		},
	})
}

func main() {
	ctx := context.Background()

	baseURL := os.Getenv("BASE_URL")
	if baseURL == "" {
		log.Fatal("BASE_URL is not set")
	}

	jwtSecret := os.Getenv("JWT_SECRET")
	if jwtSecret == "" {
		log.Fatal("JWT_SECRET is not set")
	}

	// ─── Database ───
	pool, err := pgxpool.New(ctx, os.Getenv("DATABASE_URL"))
	if err != nil {
		log.Fatalf("Unable to create a pool: %s", err)
	}
	defer pool.Close()

	queries := database.New(pool)

	// ─── Service ───
	sflkGen, err := snowflake.NewGenerator(1, 1)
	if err != nil {
		log.Fatalf("Unable to create a snowflake generator: %s", err)
	}
	// userClient, err := client.NewClient(
	// 	os.Getenv("USER_SERVICE_URL"),
	// 	&http.Client{Timeout: 5 * time.Second},
	// )
	fakeUserTunnel, err := tunnel.NewFakeTunnel(
		os.Getenv("USER_SERVICE_URL"),
		&http.Client{Timeout: 5 * time.Second},
	)
	if err != nil {
		log.Fatalf("Unable to create an user client: %s", err)
	}

	svc, err := core.NewService(queries, sflkGen, fakeUserTunnel)
	if err != nil {
		log.Fatalf("Unable to create a service: %s", err)
	}

	// ─── Consumer RabbitMQ ───
	consumer, err := broker.NewConsumer()
	if err != nil {
		log.Fatalf("Unable to create a rabbitMQ consumer: %s", err)
	}
	go consumer.Run(svc)

	// ─── Handler Endpoint ───
	handler, err := api.NewHandler(svc)
	if err != nil {
		log.Fatalf("Unable to create a http handler: %s", err)
	}

	// ─── Server HTTP ───
	srv := &http.Server{
		Addr:              ":" + os.Getenv("APP_PORT"),
		Handler:           handler.Routes(jwtSecret),
		ReadHeaderTimeout: 5 * time.Second,
		ReadTimeout:       15 * time.Second,
		WriteTimeout:      20 * time.Second,
		IdleTimeout:       120 * time.Second,
	}

	log.Fatal(srv.ListenAndServe())
	//TODO: en cas de deco-reco du service, il faut le rebrancher (le channel go du conn va quitter et la boucle va se terminer)
}
