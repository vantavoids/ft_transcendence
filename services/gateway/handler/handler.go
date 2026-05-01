// Package handler provides HTTP handlers for the gateway service
package handler

import (
	"fmt"
	"net/http"
	"net/http/httputil"
	"net/url"
	"strings"

	"github.com/vantavoids/ft_transcendence/services/gateway/config"
	"github.com/vantavoids/ft_transcendence/services/gateway/utils"
)

type Proxies map[string]*httputil.ReverseProxy

func InitProxies(s config.Services) (Proxies, error) {

	auth, err := newProxy("auth", s.Auth)
	if err != nil {
		return nil, err
	}
	chat, err := newProxy("chat", s.Chat)
	if err != nil {
		return nil, err
	}
	guild, err := newProxy("guild", s.Guild)
	if err != nil {
		return nil, err
	}
	notification, err := newProxy("notification", s.Notification)
	if err != nil {
		return nil, err
	}
	user, err := newProxy("user", s.User)
	if err != nil {
		return nil, err
	}

	return Proxies{
		"auth":         auth,
		"chat":         chat,
		"guild":        guild,
		"notification": notification,
		"user":         user,
	}, nil
}

func newProxy(name, addr string) (*httputil.ReverseProxy, error) {

	target, err := url.Parse(addr)
	if err != nil {
		return nil, fmt.Errorf("init %s proxy: %w", name, err)
	}

	return &httputil.ReverseProxy{
		Rewrite: func(preq *httputil.ProxyRequest) {

			preq.Out.URL.Scheme = target.Scheme
			preq.Out.URL.Host = target.Host
			preq.Out.Host = target.Host
			p := strings.SplitN(preq.Out.URL.Path, "/", 4)
			if len(p) >= 4 {
				preq.Out.URL.Path = "/" + p[3]
			} else {
				preq.Out.URL.Path = "/"
			}
			preq.SetXForwarded()
		},
	}, nil
}

func Redirect(proxies Proxies) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {

		parts := strings.Split(r.URL.Path, "/")
		if len(parts) < 3 {
			http.NotFound(w, r)
			return
		}

		proxy, ok := proxies[parts[2]]
		if !ok {
			http.NotFound(w, r)
			return
		}

		// log
		fmt.Println("Redirect done, forwarding to " + utils.BlueStr(parts[2]))
		proxy.ServeHTTP(w, r)
	}
}
