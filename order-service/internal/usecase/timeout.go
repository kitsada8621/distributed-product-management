package usecase

import (
	"context"
	"errors"
	"log/slog"
	"time"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain/event"
	"github.com/kitsada8621/distributed-product-management/order-service/pkg/correlation"
)

const staleBatchSize = 100

func (s *Service) RejectStale(ctx context.Context, timeout time.Duration) (int, error) {
	stale, err := s.store.FindStale(ctx, s.now().Add(-timeout), staleBatchSize)
	if err != nil {
		return 0, err
	}

	rejected := 0
	var firstErr error
	for _, candidate := range stale {
		done, err := s.timeoutOne(ctx, candidate.ID)
		if err != nil {
			slog.ErrorContext(ctx, "reject stale order failed", "orderId", candidate.ID, "error", err)
			if firstErr == nil {
				firstErr = err
			}
			continue
		}
		if done {
			rejected++
		}
	}
	return rejected, firstErr
}

func (s *Service) timeoutOne(ctx context.Context, id string) (bool, error) {
	rejected := false
	err := s.store.WithinTx(ctx, func(ctx context.Context, tx domain.Repository) error {
		o, err := tx.GetByID(ctx, id)
		if err != nil {
			return err
		}

		now := s.now()
		before := o.State()
		if !o.Timeout(now) {
			return nil
		}
		if err := tx.Save(ctx, o, before); err != nil {
			return err
		}
		msg := event.NewOrderCancelled(o.ID, event.ReasonTimeout, correlation.From(ctx), now)
		if err := tx.Enqueue(ctx, msg); err != nil {
			return err
		}
		rejected = true
		return nil
	})
	if errors.Is(err, domain.ErrStateChanged) {
		return false, nil
	}
	return rejected, err
}
