package middleware

import (
	"context"
	"net"
	"net/http"
	"strings"

	"github.com/vantavoids/ft_transcendence/services/gateway/logs"
	"github.com/vantavoids/ft_transcendence/services/gateway/utils"
)

type SourceAddrKey struct{}

func Dispatch(bypassHandler http.Handler, fullpathHandler http.Handler) http.HandlerFunc {

	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {

		ctx := updateContextSource(r)

		// log
		host := SourceAddrFromCtx(ctx)
		logs.Info(host, "Request to "+utils.BlueStr(r.URL.String()))

		bypass := func(path string) bool {
			return path == "/api/openapi.json" ||
				strings.HasPrefix(path, "/api/gateway/")
		}

		if bypass(r.URL.Path) {
			ctx = updateContextSpecial(ctx)
			bypassHandler.ServeHTTP(w, r.WithContext(ctx)) // skip the middlewares except rate limit
			return
		}
		fullpathHandler.ServeHTTP(w, r.WithContext(ctx)) // full chain
	})
}

func updateContextSource(r *http.Request) context.Context {

	// logs.Debug("X-Real-IP", r.Header.Get("X-Real-IP"))
	// logs.Debug("X-Forwarded-For", r.Header.Get("X-Forwarded-For"))

	source := strings.TrimSpace(r.Header.Get("X-Real-IP"))
	if source != "" {
		logs.Debug(source, "found X-Real-IP")
		return context.WithValue(r.Context(), SourceAddrKey{}, source)
	}

	xff := r.Header.Get("X-Forwarded-For")
	if xff != "" {
		parts := strings.Split(xff, ",")
		source = strings.TrimSpace(parts[0])
		if source != "" {
			logs.Debug(source, "found X-Forwarded-For")
			return context.WithValue(r.Context(), SourceAddrKey{}, source)
		}
	}

	host, _, err := net.SplitHostPort(r.RemoteAddr)
	if err != nil {
		logs.Debug(r.RemoteAddr, "failed to split RemoteAddr, using raw value")
		source = r.RemoteAddr
	} else {
		source = host
	}

	logs.Debug(source, "using r.RemoteAddr")
	return context.WithValue(r.Context(), SourceAddrKey{}, source)
}

func updateContextSpecial(c context.Context) context.Context {

	return context.WithValue(c, serviceKey{}, "special")
}
