package middleware

import (
	"fmt"
	"net/http"
	"time"
)

func TimeoutCat(next http.Handler) http.Handler {

	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {

		category, ok := r.Context().Value(timeoutCatKey{}).(TimeoutCategory)
		if !ok {
			fmt.Printf("missing timeout category, default to CatJSON")
			category = CatJSON
		}
		rc := http.NewResponseController(w)

		switch category {

		// Defensive but redundant: when httputil.ReverseProxy upgrades to WS,
		// it calls (http.Hijacker).Hijack() on the ResponseWriter, which clears
		// the conn's deadlines via SetDeadline(time.Time{}) and transitions
		// the conn to StateHijacked so server timeouts no longer apply.
		case CatWebSocket:
			_ = rc.SetWriteDeadline(time.Time{})
			_ = rc.SetReadDeadline(time.Time{})

		case CatUpload:
			r.Body = http.MaxBytesReader(w, r.Body, 5<<20) // 5 MB
			_ = rc.SetReadDeadline(time.Now().Add(60 * time.Second))
			_ = rc.SetWriteDeadline(time.Now().Add(30 * time.Second))

		default: // CatJSON
			r.Body = http.MaxBytesReader(w, r.Body, 1<<20) // 1 MB
			_ = rc.SetReadDeadline(time.Now().Add(5 * time.Second))
			_ = rc.SetWriteDeadline(time.Now().Add(10 * time.Second))
		}

		next.ServeHTTP(w, r)
	})
}
