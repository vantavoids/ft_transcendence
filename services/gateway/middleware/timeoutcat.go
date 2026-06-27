package middleware

import (
	"net/http"
	"time"

	"github.com/vantavoids/ft_transcendence/services/gateway/logs"
)

func TimeoutCat(next http.Handler) http.Handler {

	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {

		ctx := r.Context()
		host := SourceAddrFromCtx(ctx)

		category, ok := r.Context().Value(timeoutCatKey{}).(TimeoutCategory)
		if !ok {
			logs.Warning(host, "missing timeout category, default to CatJSON")
			category = CatJSON
		}
		rc := http.NewResponseController(w)

		switch category {

		// Defensive but redundant: when httputil.ReverseProxy upgrades to WS,
		// it calls (http.Hijacker).Hijack() on the ResponseWriter, which clears
		// the conn's deadlines via SetDeadline(time.Time{}) and transitions
		// the conn to StateHijacked so server timeouts no longer apply.
		case CatWebSocket:
			logs.Info(host, "Attributed timeout category WebSocket...")
			_ = rc.SetWriteDeadline(time.Time{})
			_ = rc.SetReadDeadline(time.Time{})

		case CatUpload:
			logs.Info(host, "Attributed timeout category Upload...")
			r.Body = http.MaxBytesReader(w, r.Body, 26<<20) // 26 MB
			_ = rc.SetReadDeadline(time.Now().Add(60 * time.Second))
			_ = rc.SetWriteDeadline(time.Now().Add(30 * time.Second))

		default:
			logs.Info(host, "Attributed timeout category JSON...")
			r.Body = http.MaxBytesReader(w, r.Body, 1<<20) // 1 MB
			_ = rc.SetReadDeadline(time.Now().Add(5 * time.Second))
			_ = rc.SetWriteDeadline(time.Now().Add(10 * time.Second))
		}

		next.ServeHTTP(w, r)
	})
}
