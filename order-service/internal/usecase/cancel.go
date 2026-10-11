package usecase

import (
	"context"
	"errors"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain/event"
	"github.com/kitsada8621/distributed-product-management/order-service/pkg/correlation"
)

func (s *Service) CancelOrder(ctx context.Context, id, userID string) (*domain.Order, error) {
	var cancelled *domain.Order
	err := s.store.WithinTx(ctx, func(ctx context.Context, tx domain.Repository) error {
		o, err := tx.GetOwned(ctx, id, userID)
		if err != nil {
			return mapNotFound(err)
		}

		now := s.now()
		before := o.State()
		if err := o.Cancel(now); err != nil {
			return err
		}
		if err := tx.Save(ctx, o, before); err != nil {
			return mapStateChanged(err, domain.CodeCannotCancel)
		}
		msg := event.NewOrderCancelled(o.ID, event.ReasonUserCancelled, correlation.From(ctx), now)
		if err := tx.Enqueue(ctx, msg); err != nil {
			return err
		}
		cancelled = o
		return nil
	})
	return cancelled, err
}

func mapStateChanged(err error, code string) error {
	if errors.Is(err, domain.ErrStateChanged) {
		return &domain.Error{Code: code, Message: "Order state changed"}
	}
	return err
}
