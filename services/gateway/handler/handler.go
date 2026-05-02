// Package handler provides HTTP handlers for the gateway service
package handler

import (
	"fmt"
	"net"
	"net/http"
	"net/http/httputil"
	"net/url"
	"strings"
	"time"

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

var dialer = &net.Dialer{
	Timeout:   5 * time.Second,  // max time to establish a TCP connection (SYN to ESTABLISHED)
	KeepAlive: 30 * time.Second, // OS-level TCP keepalive probe interval to detect dead peers
}

var transport = &http.Transport{
	DialContext:           dialer.DialContext, // function used to open new TCP connections
	MaxIdleConns:          500,                // total idle conns kept in the pool across all backends
	MaxIdleConnsPerHost:   100,                // idle conns kept per backend host (defaults to 2 if unset)
	IdleConnTimeout:       90 * time.Second,   // close idle conns after this; keep below backend's idle timeout
	ResponseHeaderTimeout: 30 * time.Second,   // max wait from request sent to first response header byte
	DisableCompression:    true,               // don't auto-add Accept-Encoding gzip; let client's header pass through
}

func newProxy(name, addr string) (*httputil.ReverseProxy, error) {

	target, err := url.Parse(addr)
	if err != nil {
		return nil, fmt.Errorf("init %s proxy: %w", name, err)
	}
	if target.Scheme == "" || target.Host == "" {
		return nil, fmt.Errorf("init %s proxy: invalid address %q", name, addr)
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

		Transport: transport,
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
