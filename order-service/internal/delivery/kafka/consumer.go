package kafka

import (
	"context"
	"fmt"
	"log/slog"
	"time"

	"github.com/twmb/franz-go/pkg/kgo"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain/event"
)

const retryPause = time.Second

type processor interface {
	Process(ctx context.Context, in Incoming) error
}

type Consumer struct {
	client    *kgo.Client
	processor processor
	logger    *slog.Logger
}

func NewConsumer(brokers []string, groupID string, topics []string, p processor, logger *slog.Logger) (*Consumer, error) {
	client, err := kgo.NewClient(
		kgo.SeedBrokers(brokers...),
		kgo.ConsumerGroup(groupID),
		kgo.ConsumeTopics(topics...),
		kgo.DisableAutoCommit(),
		kgo.ConsumeResetOffset(kgo.NewOffset().AtStart()),
	)
	if err != nil {
		return nil, fmt.Errorf("create kafka consumer: %w", err)
	}
	return &Consumer{client: client, processor: p, logger: logger}, nil
}

func (c *Consumer) Close() {
	c.client.Close()
}

func (c *Consumer) Run(ctx context.Context) error {
	for ctx.Err() == nil {
		fetches := c.client.PollFetches(ctx)
		for _, fetchErr := range fetches.Errors() {
			if ctx.Err() == nil {
				c.logger.Error("kafka fetch failed", "topic", fetchErr.Topic, "error", fetchErr.Err)
			}
		}

		for _, record := range fetches.Records() {
			if !c.handle(ctx, record) {
				return nil
			}
		}
	}
	return nil
}

func (c *Consumer) handle(ctx context.Context, record *kgo.Record) bool {
	incoming := Incoming{
		Topic:         record.Topic,
		Key:           string(record.Key),
		Value:         record.Value,
		CorrelationID: headerValue(record, event.CorrelationHeader),
	}
	for {
		err := c.processor.Process(ctx, incoming)
		if err == nil {
			break
		}
		if ctx.Err() != nil {
			return false
		}
		c.logger.Error("kafka record not processed, will retry",
			"topic", record.Topic, "offset", record.Offset, "correlationId", incoming.CorrelationID, "error", err)
		select {
		case <-time.After(retryPause):
		case <-ctx.Done():
			return false
		}
	}

	if err := c.client.CommitRecords(ctx, record); err != nil && ctx.Err() == nil {
		c.logger.Error("kafka commit failed", "topic", record.Topic, "offset", record.Offset, "error", err)
	}
	return true
}

func headerValue(record *kgo.Record, key string) string {
	for _, header := range record.Headers {
		if header.Key == key {
			return string(header.Value)
		}
	}
	return ""
}
