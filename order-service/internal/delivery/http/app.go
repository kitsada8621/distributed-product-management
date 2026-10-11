package httpserver

import (
	"log/slog"

	"github.com/gofiber/fiber/v3"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/delivery/http/dto"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/delivery/http/middleware"
)

func NewApp(logger *slog.Logger) *fiber.App {
	app := fiber.New(fiber.Config{ErrorHandler: middleware.ErrorHandler})
	app.Use(middleware.Correlation())
	app.Use(middleware.AccessLog(logger))
	app.Get("/api/orders/health", func(c fiber.Ctx) error {
		return dto.Success(c, fiber.StatusOK, "OK", map[string]string{"status": "ok"}, nil)
	})
	return app
}
