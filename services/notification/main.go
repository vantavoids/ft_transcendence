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
