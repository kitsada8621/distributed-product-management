package usecase

import (
	"context"
	"errors"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain"
)

func ParseStatus(value string) (*domain.Status, error) {
	if value == "" {
		return nil, nil
	}
	status := domain.Status(value)
	switch status {
	case domain.Pending, domain.Confirmed, domain.Rejected, domain.Cancelled:
		return &status, nil
	}
	return nil, domain.NewValidationError(domain.FieldError{Field: "status", Code: "INVALID_FORMAT", Message: "status is not a known order status"})
}

func (s *Service) ListMine(ctx context.Context, userID string, f domain.ListFilter) (domain.OrderPage, error) {
	return s.store.ListByUser(ctx, userID, f)
}

func (s *Service) ListAll(ctx context.Context, f domain.ListFilter) (domain.OrderPage, error) {
	return s.store.ListAll(ctx, f)
}

func (s *Service) GetMine(ctx context.Context, id, userID string) (*domain.Order, error) {
	o, err := s.store.GetOwned(ctx, id, userID)
	return o, mapNotFound(err)
}

func (s *Service) GetAny(ctx context.Context, id string) (*domain.Order, error) {
	o, err := s.store.GetByID(ctx, id)
	return o, mapNotFound(err)
}

func mapNotFound(err error) error {
	if errors.Is(err, domain.ErrNotFound) {
		return &domain.Error{Code: domain.CodeNotFound, Message: "Order not found"}
	}
	return err
}
