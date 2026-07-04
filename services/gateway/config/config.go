// Package config provides configuration from the env for the gateway service
package config

import (
	"fmt"
	"os"
	"strconv"
)

type Config struct {
	Dev         bool
	Debug       bool
	SocketPath  string
	MetricsPort string
	JWTSecret   string
	Limits      Limits
	Services    Services
}

type Limits struct {
	RateUID   float64
	BucketUID int
	IdleUID   int
	RateIP    float64
	BucketIP  int
	IdleIP    int
}

type Services struct {
	Auth         string
	Chat         string
	Guild        string
	Notification string
	User         string
}

func (s Services) Map() map[string]string {

	return map[string]string{
		"auth":         s.Auth,
		"chat":         s.Chat,
		"guild":        s.Guild,
		"notification": s.Notification,
		"user":         s.User,
	}
}

func (s Services) Slice() []string {

	return []string{
		"auth",
		"chat",
		"guild",
		"notification",
		"user",
	}
}

func Load() (*Config, error) {

	secret, err := requireEnv("JWT_SECRET")
	if err != nil {
		return nil, err
	}

	limits, err := loadLimits()
	if err != nil {
		return nil, err
	}

	services, err := loadServices()
	if err != nil {
		return nil, err
	}

	dev, err := envOrDefaultBool("GATEWAY_DEV", false)
	if err != nil {
		return nil, err
	}

	debug, err := envOrDefaultBool("GATEWAY_DEBUG", false)
	if err != nil {
		return nil, err
	}

	return &Config{
		Dev:         dev,
		Debug:       debug,
		SocketPath:  envOrDefaultStr("GATEWAY_SOCKET", "/run/gateway/gateway.sock"),
		MetricsPort: envOrDefaultStr("GATEWAY_METRICS_PORT", "9090"),
		JWTSecret:   secret,
		Limits:      limits,
		Services:    services,
	}, nil
}

func loadLimits() (Limits, error) {

	rateUID, err := requireEnvFloat("UID_RATE")
	if err != nil {
		return Limits{}, err
	}
	bucketUID, err := requireEnvInt("UID_BUCKET")
	if err != nil {
		return Limits{}, err
	}
	idleUID, err := requireEnvInt("UID_MAX_IDLE")
	if err != nil {
		return Limits{}, err
	}
	rateIP, err := requireEnvFloat("IP_RATE")
	if err != nil {
		return Limits{}, err
	}
	bucketIP, err := requireEnvInt("IP_BUCKET")
	if err != nil {
		return Limits{}, err
	}
	idleIP, err := requireEnvInt("IP_MAX_IDLE")
	if err != nil {
		return Limits{}, err
	}

	return Limits{
		RateUID:   rateUID,
		BucketUID: bucketUID,
		IdleUID:   idleUID,
		RateIP:    rateIP,
		BucketIP:  bucketIP,
		IdleIP:    idleIP,
	}, nil
}

func loadServices() (Services, error) {

	auth, err := requireEnv("AUTH_SERVICE_URL")
	if err != nil {
		return Services{}, err
	}
	chat, err := requireEnv("CHAT_SERVICE_URL")
	if err != nil {
		return Services{}, err
	}
	guild, err := requireEnv("GUILD_SERVICE_URL")
	if err != nil {
		return Services{}, err
	}
	notif, err := requireEnv("NOTIFICATION_SERVICE_URL")
	if err != nil {
		return Services{}, err
	}
	user, err := requireEnv("USER_SERVICE_URL")
	if err != nil {
		return Services{}, err
	}

	return Services{
		Auth:         auth,
		Chat:         chat,
		Guild:        guild,
		Notification: notif,
		User:         user,
	}, nil
}

func requireEnv(key string) (string, error) {

	val := os.Getenv(key)
	if val == "" {
		return "", fmt.Errorf("missing env: %s", key)
	}
	return val, nil
}

func envOrDefaultStr(key, fallback string) string {

	if val := os.Getenv(key); val != "" {
		return val
	}
	return fallback
}

func envOrDefaultBool(key string, fallback bool) (bool, error) {

	val := os.Getenv(key)
	if val == "" {
		return fallback, nil
	}

	parsed, err := strconv.ParseBool(val)
	if err != nil {
		return false, fmt.Errorf("invalid %s: %w", key, err)
	}

	return parsed, nil
}

func requireEnvInt(key string) (int, error) {

	val, err := requireEnv(key)
	if err != nil {
		return 0, err
	}
	valInt, err := strconv.Atoi(val)
	if err != nil {
		return 0, fmt.Errorf("invalid %s: %w", key, err)
	}
	if valInt <= 0 {
		return 0, fmt.Errorf("invalid %s: %v", key, valInt)
	}
	return valInt, nil
}

func requireEnvFloat(key string) (float64, error) {

	val, err := requireEnv(key)
	if err != nil {
		return 0.0, err
	}
	valFloat, err := strconv.ParseFloat(val, 64)
	if err != nil {
		return 0.0, fmt.Errorf("invalid %s: %w", key, err)
	}
	if valFloat <= 0.0 {
		return 0.0, fmt.Errorf("invalid %s: %v", key, valFloat)
	}
	return valFloat, nil
}
