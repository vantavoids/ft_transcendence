// Package config provides configuration from the env for the gateway service
package config

import "os"

func GetServices() map[string]string {

	return map[string]string{
		"auth":         getEnvOr("AUTH_SERVICE_URL", "http://auth:8080"),
		"chat":         getEnvOr("CHAT_SERVICE_URL", "http://chat:8080"),
		"guild":        getEnvOr("GUILD_SERVICE_URL", "http://guild:8080"),
		"notification": getEnvOr("NOTIFICATION_SERVICE_URL", "http://notification:8080"),
		"user":         getEnvOr("USER_SERVICE_URL", "http://user:8080"),
	}
}

func getEnvOr(key, fallback string) string {

	if v := os.Getenv(key); v != "" {
		return v
	}
	return fallback
}
