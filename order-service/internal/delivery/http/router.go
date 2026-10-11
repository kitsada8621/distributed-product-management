package httpserver

import (
	"github.com/gofiber/fiber/v3"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/delivery/http/handler"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/delivery/http/middleware"
)

func Register(app *fiber.App, h *handler.Handler, authenticate fiber.Handler) {
	registerAdmin(app, h, authenticate)
	app.Post("/api/orders", authenticate, h.CreateOrder)
	app.Get("/api/orders", authenticate, h.ListOrders)
	registerOwn(app, h, authenticate)
}

func registerAdmin(app *fiber.App, h *handler.Handler, authenticate fiber.Handler) {
	admin := middleware.RequireAdmin()
	app.Get("/api/orders/admin", authenticate, admin, h.AdminListOrders)
	app.Get("/api/orders/admin/:id", authenticate, admin, h.AdminGetOrder)
	app.Post("/api/orders/:id/confirm", authenticate, admin, h.ConfirmOrder)
	app.Post("/api/orders/:id/reject", authenticate, admin, h.RejectOrder)
}

func registerOwn(app *fiber.App, h *handler.Handler, authenticate fiber.Handler) {
	app.Get("/api/orders/:id", authenticate, h.GetOrder)
	app.Post("/api/orders/:id/cancel", authenticate, h.CancelOrder)
}
