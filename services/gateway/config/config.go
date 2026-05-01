// Package config provides configuration from the env for the gateway service
package config

import (
	"fmt"
	"os"
	"strconv"
)

type Config struct {
	Port      string
	JWTSecret string
	Limits    Limits
	Services  Services
}

type Limits struct {
	RateUID   float64
	BucketUID int
	RateIP    float64
	BucketIP  int
}

type Services struct {
	Auth         string
	Chat         string
	Guild        string
	Notification string
	User         string
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

	return &Config{
		Port:      envOrDefault("GATEWAY_PORT", "8080"),
		JWTSecret: secret,
		Limits:    limits,
		Services:  services,
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
	rateIP, err := requireEnvFloat("IP_RATE")
	if err != nil {
		return Limits{}, err
	}
	bucketIP, err := requireEnvInt("IP_BUCKET")
	if err != nil {
		return Limits{}, err
	}

	return Limits{
		RateUID:   rateUID,
		BucketUID: bucketUID,
		RateIP:    rateIP,
		BucketIP:  bucketIP,
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

func envOrDefault(key, fallback string) string {

	if val := os.Getenv(key); val != "" {
		return val
	}
	return fallback
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
