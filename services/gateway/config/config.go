// Package config provides configuration from the env for the gateway service
package config

import (
	"log"
	"os"
	"strings"
)

func GetServices() map[string]string {

	var services = []string{"auth", "chat", "guild", "notification", "user"}
	var ret = map[string]string{}

	for _, name := range services {
		url := os.Getenv(strings.ToUpper(name) + "_SERVICE_URL")
		if url == "" {
			log.Fatalf("missing env: %s_SERVICE_URL", strings.ToUpper(name))
		}
		ret[name] = url
	}

	return ret
}
