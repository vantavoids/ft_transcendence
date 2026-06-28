package main

import (
	"context"
	"log"
	"net/http"
	"os"
	"time"

	pgxpool "github.com/jackc/pgx/v5/pgxpool"
	database "github.com/vantavoids/ft_transcendence/services/notification/db/sqlc"
	core "github.com/vantavoids/ft_transcendence/services/notification/internal/core"
	snowflake "github.com/vantavoids/ft_transcendence/services/notification/internal/platform/snowflake"
	tunnel "github.com/vantavoids/ft_transcendence/services/notification/internal/platform/tunnel"
	api "github.com/vantavoids/ft_transcendence/services/notification/transport/api"
	broker "github.com/vantavoids/ft_transcendence/services/notification/transport/broker"
)

func main() {
	ctx := context.Background()

	userServiceURL := os.Getenv("USER_SERVICE_URL")
	if userServiceURL == "" {
		log.Fatal("USER_SERVICE_URL is not set")
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

	// ─── Service ───
	queries := database.New(pool)

	hub, err := core.NewHub()
	if err != nil {
		log.Fatalf("Unable to create a hub: %s", err)
	}

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

	orch, err := core.NewOrchestrator(hub, queries, sflkGen, fakeUserTunnel)
	if err != nil {
		log.Fatalf("Unable to create a service: %s", err)
	}

	// ─── Consumer RabbitMQ ───
	consumer, err := broker.NewConsumer()
	if err != nil {
		log.Fatalf("Unable to create a rabbitMQ consumer: %s", err)
	}
	go consumer.Run(orch)

	// ─── Handler Endpoint ───
	handler, err := api.NewHandler(orch, hub)
	if err != nil {
		log.Fatalf("Unable to create a http handler: %s", err)
	}

	core.RunCleanupLoop(ctx, orch)

	// ─── Server HTTP ───
	srv := &http.Server{
		Addr:              ":8080",
		Handler:           handler.Routes(jwtSecret),
		ReadHeaderTimeout: 5 * time.Second,
		ReadTimeout:       15 * time.Second,
		WriteTimeout:      20 * time.Second,
		IdleTimeout:       120 * time.Second,
	}

	log.Fatal(srv.ListenAndServe())
	//TODO: en cas de deco-reco du service, il faut le rebrancher (le channel go du conn va quitter et la boucle va se terminer)
}
