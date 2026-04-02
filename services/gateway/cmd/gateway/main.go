package main

import (
	"log"
	"net/http"

	"github.com/vantavoids/ft_transcendence/services/gateway/handler"
)

func main() {

	mux := http.NewServeMux()

	mux.HandleFunc("/hello", handler.Hello)
	mux.HandleFunc("/headers", handler.Headers)

	log.Fatal(http.ListenAndServe(":8090", mux))
}
