package kafka

import (
	"context"
	"log/slog"
	"time"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain/event"
)

type dispatcher interface {
	Dispatch(ctx context.Context, in Incoming) error
}

type DeadLetterSink interface {
	Enqueue(ctx context.Context, msg event.Message) error
}

type Processor struct {
	dispatcher  dispatcher
	sink        DeadLetterSink
	maxAttempts int
	retryDelay  time.Duration
	now         func() time.Time
}

func NewProcessor(d dispatcher, sink DeadLetterSink, maxAttempts int, retryDelay time.Duration, now func() time.Time) *Processor {
	return &Processor{dispatcher: d, sink: sink, maxAttempts: maxAttempts, retryDelay: retryDelay, now: now}
}

func (p *Processor) Process(ctx context.Context, in Incoming) error {
	var lastErr error
	for attempt := 1; attempt <= p.maxAttempts; attempt++ {
		lastErr = p.dispatcher.Dispatch(ctx, in)
		if lastErr == nil {
			return nil
		}
		slog.WarnContext(ctx, "message handling failed",
			"topic", in.Topic, "attempt", attempt, "correlationId", in.CorrelationID, "error", lastErr)

		if attempt < p.maxAttempts && !sleep(ctx, p.retryDelay*time.Duration(attempt)) {
			return ctx.Err()
		}
	}

	dead := event.NewDeadLetter(in.Topic, in.Key, in.Value, lastErr.Error(), p.maxAttempts, in.CorrelationID, p.now())
	if err := p.sink.Enqueue(ctx, dead); err != nil {
		return err
	}
	slog.ErrorContext(ctx, "message moved to dead letter",
		"topic", dead.Topic, "attempts", p.maxAttempts, "correlationId", in.CorrelationID, "error", lastErr)
	return nil
}

func sleep(ctx context.Context, delay time.Duration) bool {
	if delay <= 0 {
		return ctx.Err() == nil
	}
	timer := time.NewTimer(delay)
	defer timer.Stop()
	select {
	case <-timer.C:
		return true
	case <-ctx.Done():
		return false
	}
}
