package worker

import (
	"context"
	"log/slog"
	"time"
)

func Every(ctx context.Context, name string, interval time.Duration, logger *slog.Logger, fn func(ctx context.Context) error) {
	ticker := time.NewTicker(interval)
	defer ticker.Stop()

	for {
		if err := fn(ctx); err != nil && ctx.Err() == nil {
			logger.Error("worker run failed", "worker", name, "error", err)
		}

		select {
		case <-ctx.Done():
			return
		case <-ticker.C:
		}
	}
}
