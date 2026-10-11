package domain

import (
	"context"
	"time"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain/event"
)

type IdempotencyRecord struct {
	RequestHash string
	Order       *Order
}

type OrderRow struct {
	ID          string
	OwnerEmail  string
	Status      Status
	StockStatus StockStatus
	TotalMinor  *int64
	CreatedAt   time.Time
}

type ListFilter struct {
	Status   *Status
	Page     int
	PageSize int
}

type OrderPage struct {
	Rows  []OrderRow
	Total int
}

type Repository interface {
	FindIdempotency(ctx context.Context, userID, key string) (*IdempotencyRecord, error)

	Create(ctx context.Context, o *Order, key, requestHash string) error
	GetByID(ctx context.Context, id string) (*Order, error)

	GetOwned(ctx context.Context, id, userID string) (*Order, error)

	Save(ctx context.Context, o *Order, before State) error

	FindStale(ctx context.Context, createdBefore time.Time, limit int) ([]*Order, error)
	ListByUser(ctx context.Context, userID string, f ListFilter) (OrderPage, error)
	ListAll(ctx context.Context, f ListFilter) (OrderPage, error)

	Enqueue(ctx context.Context, msg event.Message) error

	MarkProcessed(ctx context.Context, eventID, topic string) (firstTime bool, err error)
}

type Store interface {
	Repository

	WithinTx(ctx context.Context, fn func(ctx context.Context, tx Repository) error) error
}

type OutboxEntry struct {
	event.Message
	Attempts int
}

type OutboxStore interface {
	FetchDue(ctx context.Context, now time.Time, limit int) ([]OutboxEntry, error)
	MarkSent(ctx context.Context, id string, at time.Time) error
	MarkFailed(ctx context.Context, id string, attempts int, next time.Time) error
}
