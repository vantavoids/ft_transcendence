// Package handler provides HTTP handlers for the gateway service
package handler

import (
	"fmt"
	"net/http"
)

func Redirect(w http.ResponseWriter, r *http.Request) {

	// todo...
	fmt.Println("- Routing todo -")
}

// func Headers(w http.ResponseWriter, req *http.Request) {
//
// 	for name, headers := range req.Header {
// 		for _, h := range headers {
// 			fmt.Fprintf(w, "%v: %v\n", name, h)
// 		}
// 	}
// }
