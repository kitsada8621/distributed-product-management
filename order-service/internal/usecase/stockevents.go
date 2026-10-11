package usecase

import (
	"context"
	"fmt"
	"log/slog"
	"math"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain/event"
)

func (s *Service) HandleStockReserved(ctx context.Context, ev event.StockReserved) error {
	items := make([]domain.ReservedItem, 0, len(ev.Items))
	for _, item := range ev.Items {
		items = append(items, domain.ReservedItem{
			ProductID:      item.ProductID,
			Name:           item.Name,
			UnitPriceMinor: int64(math.Round(item.UnitPrice * 100)),
		})
	}

	return s.store.WithinTx(ctx, func(ctx context.Context, tx domain.Repository) error {
		o, proceed, err := s.startEvent(ctx, tx, ev.EventID, event.TopicStockReserved, ev.OrderID)
		if err != nil || !proceed {
			return err
		}

		before := o.State()
		applied, err := o.ApplyReserved(items, s.now())
		if err != nil {
			return err
		}
		return s.finishEvent(ctx, tx, o, before, applied, event.TopicStockReserved)
	})
}

func (s *Service) HandleStockRejected(ctx context.Context, ev event.StockRejected) error {
	failed := make([]domain.FailedItem, 0, len(ev.FailedItems))
	for _, item := range ev.FailedItems {
		failed = append(failed, domain.FailedItem{
			ProductID: item.ProductID,
			Reason:    domain.FailureReason(item.Reason),
			Requested: item.Requested,
			Available: item.Available,
		})
	}

	return s.store.WithinTx(ctx, func(ctx context.Context, tx domain.Repository) error {
		o, proceed, err := s.startEvent(ctx, tx, ev.EventID, event.TopicStockRejected, ev.OrderID)
		if err != nil || !proceed {
			return err
		}

		before := o.State()
		applied, err := o.ApplyRejected(failed, s.now())
		if err != nil {
			return err
		}
		return s.finishEvent(ctx, tx, o, before, applied, event.TopicStockRejected)
	})
}

func (s *Service) startEvent(ctx context.Context, tx domain.Repository, eventID, topic, orderID string) (o *domain.Order, proceed bool, err error) {
	firstTime, err := tx.MarkProcessed(ctx, eventID, topic)
	if err != nil || !firstTime {
		return nil, false, err
	}
	o, err = tx.GetByID(ctx, orderID)
	if err != nil {
		return nil, false, fmt.Errorf("%s for order %s: %w", topic, orderID, err)
	}
	return o, true, nil
}

func (s *Service) finishEvent(ctx context.Context, tx domain.Repository, o *domain.Order, before domain.State, applied bool, topic string) error {
	if !applied {
		slog.InfoContext(ctx, "skip event for order that already moved on",
			"topic", topic, "orderId", o.ID, "status", o.Status, "stockStatus", o.StockStatus)
		return nil
	}
	return tx.Save(ctx, o, before)
}
