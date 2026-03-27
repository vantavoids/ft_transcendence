package main

import (
	// "fmt"
	"net/http"

	"github.com/vantavoids/ft_transcendence/services/gateway/handler"
)

func main() {

	http.HandleFunc("/hello", handler.Hello)
	http.HandleFunc("/headers", handler.Headers)

	http.ListenAndServe(":8090", nil)
}
