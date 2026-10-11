package config

import (
	"fmt"
	"strconv"
	"strings"
	"time"
)

type Config struct {
	HTTPAddr         string
	DBPath           string
	JWTIssuer        string
	JWTPublicKeyPath string
	KafkaEnabled     bool
	KafkaBrokers     []string
	KafkaGroupID     string
	RelayInterval    time.Duration
	SweeperInterval  time.Duration
	ReserveTimeout   time.Duration
	LogLevel         string
}

func Load(getenv func(string) string) (Config, error) {
	cfg := Config{
		HTTPAddr:         valueOr(getenv, "HTTP_ADDR", ":8080"),
		DBPath:           valueOr(getenv, "DB_PATH", "order.db"),
		JWTIssuer:        valueOr(getenv, "JWT_ISSUER", "auth-service"),
		JWTPublicKeyPath: getenv("JWT_PUBLIC_KEY_PATH"),
		KafkaGroupID:     valueOr(getenv, "KAFKA_GROUP_ID", "order-service"),
		LogLevel:         valueOr(getenv, "LOG_LEVEL", "info"),
	}
	if cfg.JWTPublicKeyPath == "" {
		return Config{}, fmt.Errorf("JWT_PUBLIC_KEY_PATH is required")
	}

	enabled, err := parseBool(getenv, "KAFKA_ENABLED", true)
	if err != nil {
		return Config{}, err
	}
	cfg.KafkaEnabled = enabled
	if enabled {
		cfg.KafkaBrokers = splitList(getenv("KAFKA_BROKERS"))
		if len(cfg.KafkaBrokers) == 0 {
			return Config{}, fmt.Errorf("KAFKA_BROKERS is required when KAFKA_ENABLED is true")
		}
	}

	if cfg.RelayInterval, err = parseDuration(getenv, "RELAY_INTERVAL", time.Second); err != nil {
		return Config{}, err
	}
	if cfg.SweeperInterval, err = parseDuration(getenv, "SWEEPER_INTERVAL", 10*time.Second); err != nil {
		return Config{}, err
	}
	if cfg.ReserveTimeout, err = parseDuration(getenv, "RESERVE_TIMEOUT", 60*time.Second); err != nil {
		return Config{}, err
	}
	return cfg, nil
}

func valueOr(getenv func(string) string, name, fallback string) string {
	value := getenv(name)
	if value == "" {
		return fallback
	}
	return value
}

func parseBool(getenv func(string) string, name string, fallback bool) (bool, error) {
	value := getenv(name)
	if value == "" {
		return fallback, nil
	}
	parsed, err := strconv.ParseBool(value)
	if err != nil {
		return false, fmt.Errorf("%s must be true or false: %w", name, err)
	}
	return parsed, nil
}

func parseDuration(getenv func(string) string, name string, fallback time.Duration) (time.Duration, error) {
	value := getenv(name)
	if value == "" {
		return fallback, nil
	}
	parsed, err := time.ParseDuration(value)
	if err != nil {
		return 0, fmt.Errorf("%s must be a duration such as 1s: %w", name, err)
	}
	if parsed <= 0 {
		return 0, fmt.Errorf("%s must be greater than zero", name)
	}
	return parsed, nil
}

func splitList(value string) []string {
	var items []string
	for _, part := range strings.Split(value, ",") {
		part = strings.TrimSpace(part)
		if part != "" {
			items = append(items, part)
		}
	}
	return items
}
