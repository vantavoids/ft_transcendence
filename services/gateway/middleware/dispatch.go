package middleware

import (
	"context"
	"net/http"
	"strings"

	"github.com/vantavoids/ft_transcendence/services/gateway/logs"
	"github.com/vantavoids/ft_transcendence/services/gateway/utils"
)

func Dispatch(bypassHandler http.Handler, fullpathHandler http.Handler) http.HandlerFunc {

	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {

		// log
		logs.Info(r.RemoteAddr, "Request to "+utils.BlueStr(r.URL.String()))

		bypass := func(path string) bool {
			return path == "/api/openapi.json" ||
				strings.HasPrefix(path, "/api/gateway/")
		}

		if bypass(r.URL.Path) {
			ctx := UpdateContextSpecial(r)
			bypassHandler.ServeHTTP(w, r.WithContext(ctx)) // skip the middlewares except rate limit
			return
		}
		fullpathHandler.ServeHTTP(w, r) // full chain
	})
}

func UpdateContextSpecial(r *http.Request) context.Context {

	return context.WithValue(r.Context(), serviceKey{}, "special")
}
