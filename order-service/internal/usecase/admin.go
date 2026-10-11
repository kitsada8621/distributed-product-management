package usecase

import (
	"context"
	"time"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain/event"
	"github.com/kitsada8621/distributed-product-management/order-service/pkg/correlation"
)

func (s *Service) ConfirmOrder(ctx context.Context, id string) (*domain.Order, error) {
	return s.decide(ctx, id, func(o *domain.Order, now time.Time) (event.Message, error) {
		if err := o.Confirm(now); err != nil {
			return event.Message{}, err
		}
		return event.NewOrderConfirmed(o.ID, correlation.From(ctx), now), nil
	})
}

func (s *Service) RejectOrder(ctx context.Context, id, reason string) (*domain.Order, error) {
	return s.decide(ctx, id, func(o *domain.Order, now time.Time) (event.Message, error) {
		if err := o.Reject(reason, now); err != nil {
			return event.Message{}, err
		}
		return event.NewOrderCancelled(o.ID, event.ReasonAdminRejected, correlation.From(ctx), now), nil
	})
}

func (s *Service) decide(ctx context.Context, id string, apply func(o *domain.Order, now time.Time) (event.Message, error)) (*domain.Order, error) {
	var decided *domain.Order
	err := s.store.WithinTx(ctx, func(ctx context.Context, tx domain.Repository) error {
		o, err := tx.GetByID(ctx, id)
		if err != nil {
			return mapNotFound(err)
		}

		before := o.State()
		msg, err := apply(o, s.now())
		if err != nil {
			return err
		}
		if err := tx.Save(ctx, o, before); err != nil {
			return mapStateChanged(err, domain.CodeInvalidState)
		}
		if err := tx.Enqueue(ctx, msg); err != nil {
			return err
		}
		decided = o
		return nil
	})
	return decided, err
}
