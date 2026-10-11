package handler

import (
	"context"

	"github.com/gofiber/fiber/v3"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/delivery/http/dto"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/delivery/http/middleware"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/usecase"
)

type OrderService interface {
	CreateOrder(ctx context.Context, caller domain.Caller, key string, inputs []domain.ItemInput, address domain.ShippingAddress, payment domain.PaymentMethod) (usecase.CreateResult, error)
	ListMine(ctx context.Context, userID string, f domain.ListFilter) (domain.OrderPage, error)
	GetMine(ctx context.Context, id, userID string) (*domain.Order, error)
	CancelOrder(ctx context.Context, id, userID string) (*domain.Order, error)
	ListAll(ctx context.Context, f domain.ListFilter) (domain.OrderPage, error)
	GetAny(ctx context.Context, id string) (*domain.Order, error)
	ConfirmOrder(ctx context.Context, id string) (*domain.Order, error)
	RejectOrder(ctx context.Context, id, reason string) (*domain.Order, error)
}

type Handler struct {
	orders OrderService
}

func NewHandler(orders OrderService) *Handler {
	return &Handler{orders: orders}
}

func (h *Handler) CreateOrder(c fiber.Ctx) error {
	var body dto.CreateRequest
	if err := c.Bind().JSON(&body); err != nil {
		return err
	}
	inputs := make([]domain.ItemInput, 0, len(body.Items))
	for _, item := range body.Items {
		inputs = append(inputs, domain.ItemInput{ProductID: item.ProductID, Quantity: item.Quantity})
	}

	address := domain.ShippingAddress{
		RecipientName: body.ShippingAddress.RecipientName,
		Phone:         body.ShippingAddress.Phone,
		AddressLine:   body.ShippingAddress.AddressLine,
		Province:      body.ShippingAddress.Province,
		PostalCode:    body.ShippingAddress.PostalCode,
	}

	result, err := h.orders.CreateOrder(c.Context(), middleware.CurrentCaller(c), c.Get("Idempotency-Key"), inputs, address, domain.PaymentMethod(body.PaymentMethod))
	if err != nil {
		return err
	}
	if result.Created {
		return dto.Success(c, fiber.StatusAccepted, "Order accepted", dto.ToCreateResponse(result.Order), nil)
	}
	return dto.Success(c, fiber.StatusOK, "Order already created", dto.ToCreateResponse(result.Order), nil)
}
