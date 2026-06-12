package main

import (
	"encoding/json"
	"log"
	"net/http"
	"os"
	"time"
)

type helloResponse struct {
	Status  string `json:"status"`
	Service string `json:"service"`
}

func helloWorld(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(http.StatusOK)
	json.NewEncoder(w).Encode(helloResponse{
		Status:  "ok",
		Service: "notification",
	})
}

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
	// I dont have a config loader like Cartoone for the moment
	port := os.Getenv("APP_PORT")
	if port == "" {
		port = "8080"
	}

	mux := http.NewServeMux()

	// For the moment, the endpoint hello world is in the main,
	// this will be fixed after the pull request
	mux.HandleFunc("GET /v1/hello-world", helloWorld)
	mux.HandleFunc("GET /healthz", healthz)

	srv := &http.Server{
		Addr:              ":" + port,
		Handler:           mux,
		ReadHeaderTimeout: 5 * time.Second,
		ReadTimeout:       15 * time.Second,
		WriteTimeout:      20 * time.Second,
		IdleTimeout:       120 * time.Second,
	}

	log.Printf("notification service listening on :%s", port)
	log.Fatal(srv.ListenAndServe())
}
