package kafka

import (
	"context"
	"fmt"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain/event"
	"github.com/kitsada8621/distributed-product-management/order-service/pkg/correlation"
)

type Incoming struct {
	Topic         string
	Key           string
	Value         []byte
	CorrelationID string
}

type Handler interface {
	HandleStockReserved(ctx context.Context, ev event.StockReserved) error
	HandleStockRejected(ctx context.Context, ev event.StockRejected) error
}

type Dispatcher struct {
	handler Handler
}

func NewDispatcher(h Handler) *Dispatcher {
	return &Dispatcher{handler: h}
}

func (d *Dispatcher) Dispatch(ctx context.Context, in Incoming) error {
	id := in.CorrelationID
	if id == "" {
		id = correlation.NewID()
	}
	ctx = correlation.With(ctx, id)

	switch in.Topic {
	case event.TopicStockReserved:
		ev, err := event.DecodeStockReserved(in.Value)
		if err != nil {
			return err
		}
		return d.handler.HandleStockReserved(ctx, ev)
	case event.TopicStockRejected:
		ev, err := event.DecodeStockRejected(in.Value)
		if err != nil {
			return err
		}
		return d.handler.HandleStockRejected(ctx, ev)
	}
	return fmt.Errorf("unsupported topic %q", in.Topic)
}
