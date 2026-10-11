package middleware

import (
	"log/slog"
	"time"

	"github.com/gofiber/fiber/v3"

	"github.com/kitsada8621/distributed-product-management/order-service/pkg/correlation"
)

func Correlation() fiber.Handler {
	return func(c fiber.Ctx) error {
		id := c.Get(correlation.Header)
		if id == "" {
			id = correlation.NewID()
		}
		c.Set(correlation.Header, id)
		c.SetContext(correlation.With(c.Context(), id))
		return c.Next()
	}
}

func AccessLog(logger *slog.Logger) fiber.Handler {
	return func(c fiber.Ctx) error {
		start := time.Now()
		err := c.Next()
		logger.InfoContext(c.Context(), "request",
			"method", c.Method(),
			"path", c.Path(),
			"status", c.Response().StatusCode(),
			"durationMs", time.Since(start).Milliseconds(),
			"correlationId", correlation.From(c.Context()))
		return err
	}
}
