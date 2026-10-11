package usecase

import (
	"context"
	"strings"
	"testing"
	"time"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain/event"
)

func orderCreatedAt(t *testing.T, store *fakeStore, age time.Duration, status domain.Status, stock domain.StockStatus) *domain.Order {
	t.Helper()
	o := storedOrder(t, store, status, stock)
	o.CreatedAt = testNow.Add(-age)
	return o
}

func TestRejectStaleRejectsOldReservingOrder(t *testing.T) {
	store := newFakeStore()
	o := orderCreatedAt(t, store, 61*time.Second, domain.Pending, domain.Reserving)

	count, err := newTestService(store).RejectStale(context.Background(), 60*time.Second)
	if err != nil || count != 1 {
		t.Fatalf("count=%d err=%v", count, err)
	}
	got := store.orders[o.ID]
	if got.Status != domain.Rejected || got.Rejection.Type != domain.TimeoutRejection {
		t.Errorf("order = %+v", got)
	}
	if len(store.outbox) != 1 || store.outbox[0].Topic != event.TopicOrderCancelled ||
		!strings.Contains(string(store.outbox[0].Payload), `"reason":"TIMEOUT"`) {
		t.Errorf("outbox = %+v", store.outbox)
	}
}

func TestRejectStaleLeavesOtherOrders(t *testing.T) {
	store := newFakeStore()
	young := orderCreatedAt(t, store, 30*time.Second, domain.Pending, domain.Reserving)
	reserved := orderCreatedAt(t, store, 61*time.Second, domain.Pending, domain.Reserved)
	cancelled := orderCreatedAt(t, store, 61*time.Second, domain.Cancelled, domain.Reserving)

	count, err := newTestService(store).RejectStale(context.Background(), 60*time.Second)
	if err != nil || count != 0 || len(store.outbox) != 0 {
		t.Fatalf("count=%d err=%v outbox=%d", count, err, len(store.outbox))
	}
	for _, o := range []*domain.Order{young, reserved, cancelled} {
		if store.orders[o.ID].Status != o.Status {
			t.Errorf("order %s changed", o.ID)
		}
	}
}

func TestRejectStaleRunsOnce(t *testing.T) {
	store := newFakeStore()
	orderCreatedAt(t, store, 61*time.Second, domain.Pending, domain.Reserving)
	svc := newTestService(store)

	first, _ := svc.RejectStale(context.Background(), 60*time.Second)
	second, _ := svc.RejectStale(context.Background(), 60*time.Second)
	if first != 1 || second != 0 || len(store.outbox) != 1 {
		t.Errorf("first=%d second=%d outbox=%d", first, second, len(store.outbox))
	}
}
