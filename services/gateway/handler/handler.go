// Package handler provides HTTP handlers for the gateway service
package handler

import (
	"fmt"
	"net/http"
	"net/http/httputil"
	"net/url"
	"strings"

	"github.com/vantavoids/ft_transcendence/services/gateway/utils"
)

var proxies map[string]*httputil.ReverseProxy

func InitProxies(services map[string]string) {

	proxies = make(map[string]*httputil.ReverseProxy, len(services))
	for name, addr := range services {
		proxies[name] = newProxy(addr)
	}
}

func newProxy(addr string) *httputil.ReverseProxy {

	target, err := url.Parse(addr)
	if err != nil {
		panic(err)
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
	}
}

func Redirect(w http.ResponseWriter, r *http.Request) {

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
