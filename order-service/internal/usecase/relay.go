package usecase

import (
	"context"
	"log/slog"
	"time"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain/event"
	"github.com/kitsada8621/distributed-product-management/order-service/pkg/correlation"
)

const (
	batchSize  = 50
	minBackoff = time.Second
	maxBackoff = 60 * time.Second
)

type Relay struct {
	store     domain.OutboxStore
	publisher event.Publisher
	now       func() time.Time
}

func NewRelay(store domain.OutboxStore, publisher event.Publisher, now func() time.Time) *Relay {
	return &Relay{store: store, publisher: publisher, now: now}
}

func (r *Relay) RunOnce(ctx context.Context) error {
	entries, err := r.store.FetchDue(ctx, r.now(), batchSize)
	if err != nil {
		return err
	}

	for _, entry := range entries {
		msgCtx := correlation.With(ctx, entry.CorrelationID)
		if err := r.publisher.Publish(msgCtx, entry.Message); err != nil {
			attempts := entry.Attempts + 1
			slog.WarnContext(msgCtx, "publish failed, will retry",
				"eventId", entry.ID, "topic", entry.Topic, "attempts", attempts,
				"correlationId", entry.CorrelationID, "error", err)
			return r.store.MarkFailed(ctx, entry.ID, attempts, r.now().Add(backoff(attempts)))
		}
		if err := r.store.MarkSent(ctx, entry.ID, r.now()); err != nil {
			return err
		}
	}
	return nil
}

func backoff(attempts int) time.Duration {
	delay := minBackoff
	for i := 1; i < attempts && delay < maxBackoff; i++ {
		delay *= 2
	}
	if delay > maxBackoff {
		return maxBackoff
	}
	return delay
}
