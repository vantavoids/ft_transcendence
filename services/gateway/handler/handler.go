// Package handler provides HTTP handlers for the gateway service
package handler

import (
	"net/http"
	"net/http/httputil"
	"net/url"
	"strings"
)

var proxies map[string]*httputil.ReverseProxy
var serviceURLs map[string]string

func InitProxies(services map[string]string) {

	serviceURLs = services
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

	proxy.ServeHTTP(w, r)
}
