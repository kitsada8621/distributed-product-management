package handler

import (
	"github.com/gofiber/fiber/v3"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/delivery/http/dto"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/delivery/http/middleware"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/usecase"
)

func (h *Handler) ListOrders(c fiber.Ctx) error {
	filter, err := parseListFilter(c)
	if err != nil {
		return err
	}
	page, err := h.orders.ListMine(c.Context(), middleware.CurrentCaller(c).UserID, filter)
	if err != nil {
		return err
	}
	return listResponse(c, page, filter, false)
}

func (h *Handler) GetOrder(c fiber.Ctx) error {
	o, err := h.orders.GetMine(c.Context(), c.Params("id"), middleware.CurrentCaller(c).UserID)
	if err != nil {
		return err
	}
	return dto.Success(c, fiber.StatusOK, "Order retrieved", dto.ToOrderResponse(o, false), nil)
}

func (h *Handler) CancelOrder(c fiber.Ctx) error {
	o, err := h.orders.CancelOrder(c.Context(), c.Params("id"), middleware.CurrentCaller(c).UserID)
	if err != nil {
		return err
	}
	return dto.Success(c, fiber.StatusOK, "Order cancelled", dto.ToOrderResponse(o, false), nil)
}

func parseListFilter(c fiber.Ctx) (domain.ListFilter, error) {
	status, err := usecase.ParseStatus(c.Query("status"))
	if err != nil {
		return domain.ListFilter{}, err
	}
	page, pageSize, err := dto.ParsePaging(c)
	if err != nil {
		return domain.ListFilter{}, err
	}
	return domain.ListFilter{Status: status, Page: page, PageSize: pageSize}, nil
}

func listResponse(c fiber.Ctx, page domain.OrderPage, filter domain.ListFilter, withOwner bool) error {
	rows := make([]dto.RowResponse, 0, len(page.Rows))
	for _, row := range page.Rows {
		rows = append(rows, dto.ToRowResponse(row, withOwner))
	}
	meta := dto.NewMeta(filter.Page, filter.PageSize, page.Total)
	return dto.Success(c, fiber.StatusOK, "Orders retrieved", rows, meta)
}
