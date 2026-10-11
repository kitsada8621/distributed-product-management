package handler

import (
	"github.com/gofiber/fiber/v3"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/delivery/http/dto"
)

func (h *Handler) AdminListOrders(c fiber.Ctx) error {
	filter, err := parseListFilter(c)
	if err != nil {
		return err
	}
	page, err := h.orders.ListAll(c.Context(), filter)
	if err != nil {
		return err
	}
	return listResponse(c, page, filter, true)
}

func (h *Handler) AdminGetOrder(c fiber.Ctx) error {
	o, err := h.orders.GetAny(c.Context(), c.Params("id"))
	if err != nil {
		return err
	}
	return dto.Success(c, fiber.StatusOK, "Order retrieved", dto.ToOrderResponse(o, true), nil)
}

func (h *Handler) ConfirmOrder(c fiber.Ctx) error {
	o, err := h.orders.ConfirmOrder(c.Context(), c.Params("id"))
	if err != nil {
		return err
	}
	return dto.Success(c, fiber.StatusOK, "Order confirmed", dto.ToOrderResponse(o, false), nil)
}

func (h *Handler) RejectOrder(c fiber.Ctx) error {
	var body dto.RejectRequest
	if err := c.Bind().JSON(&body); err != nil {
		return err
	}
	o, err := h.orders.RejectOrder(c.Context(), c.Params("id"), body.Reason)
	if err != nil {
		return err
	}
	return dto.Success(c, fiber.StatusOK, "Order rejected", dto.ToOrderResponse(o, false), nil)
}
