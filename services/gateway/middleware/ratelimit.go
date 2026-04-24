package middleware

import (
	"fmt"
	"net"
	"net/http"

	"github.com/vantavoids/ft_transcendence/services/gateway/utils"
)

type RateLimitStore interface {
	Allow(identity string) bool
}

func IPLimitFunc(store RateLimitStore) func(http.Handler) http.Handler {

	return func(next http.Handler) http.Handler {

		return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {

			if !r.Context().Value(isAuthKey).(bool) {
				// log
				fmt.Println("IP limit bypassed, forwarding...")
				next.ServeHTTP(w, r)
				return
			}

			host, _, err := net.SplitHostPort(r.RemoteAddr)
			if err != nil {
				errMsg := "bad request"
				http.Error(w, errMsg, http.StatusBadRequest)
				fmt.Println(utils.RedStr(errMsg) + " " + r.RemoteAddr)
				return
			}
			if !store.Allow(host) {
				errMsg := "too many requests"
				http.Error(w, errMsg, http.StatusTooManyRequests)
				fmt.Println(utils.RedStr(errMsg))
				return
			}

			// log
			fmt.Println("IP limit passed, forwarding...")
			next.ServeHTTP(w, r)
		})
	}
}

func UIDLimitFunc(store RateLimitStore) func(http.Handler) http.Handler {

	return func(next http.Handler) http.Handler {

		return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {

			if r.Context().Value(isAuthKey).(bool) {
				// log
				fmt.Println("UID limit bypassed, forwarding...")
				next.ServeHTTP(w, r)
				return
			}

			uid := r.Context().Value(subKey).(string)

			if !store.Allow(uid) {
				errMsg := "too many requests"
				http.Error(w, errMsg, http.StatusTooManyRequests)
				fmt.Println(utils.RedStr(errMsg))
				return
			}

			// log
			fmt.Println("UID limit passed, forwarding...")
			next.ServeHTTP(w, r)
		})
	}
}
