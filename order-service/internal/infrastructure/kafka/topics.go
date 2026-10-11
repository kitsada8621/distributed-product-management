package kafka

import (
	"context"
	"errors"
	"fmt"

	"github.com/twmb/franz-go/pkg/kadm"
	"github.com/twmb/franz-go/pkg/kerr"
	"github.com/twmb/franz-go/pkg/kgo"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain/event"
)

func OrderTopics() []string {
	return []string{
		event.TopicOrderCreated,
		event.TopicOrderConfirmed,
		event.TopicOrderCancelled,
		event.TopicStockReserved,
		event.TopicStockRejected,

		event.DLQPrefix + event.TopicStockReserved,
		event.DLQPrefix + event.TopicStockRejected,
	}
}

func EnsureTopics(ctx context.Context, client *kgo.Client, topics []string) error {
	responses, err := kadm.NewClient(client).CreateTopics(ctx, 1, 1, nil, topics...)
	if err != nil {
		return fmt.Errorf("create topics: %w", err)
	}
	for _, response := range responses {
		if response.Err != nil && !errors.Is(response.Err, kerr.TopicAlreadyExists) {
			return fmt.Errorf("create topic %s: %w", response.Topic, response.Err)
		}
	}
	return nil
}
