package usecase

import (
	"context"
	"errors"
	"strings"
	"testing"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain/event"
)

func storedOrder(t *testing.T, store *fakeStore, status domain.Status, stock domain.StockStatus) *domain.Order {
	t.Helper()
	o := seedOrders(t, store, "u1", 1)[0]
	store.orders[o.ID].Status = status
	store.orders[o.ID].StockStatus = stock
	return store.orders[o.ID]
}

func TestCancelPending(t *testing.T) {
	for _, stock := range []domain.StockStatus{domain.Reserving, domain.Reserved} {
		store := newFakeStore()
		o := storedOrder(t, store, domain.Pending, stock)

		got, err := newTestService(store).CancelOrder(context.Background(), o.ID, "u1")
		if err != nil || got.Status != domain.Cancelled || store.orders[o.ID].Status != domain.Cancelled {
			t.Fatalf("%s: got=%+v err=%v", stock, got, err)
		}
		if len(store.outbox) != 1 || store.outbox[0].Topic != event.TopicOrderCancelled {
			t.Fatalf("%s: outbox = %+v", stock, store.outbox)
		}
		if !strings.Contains(string(store.outbox[0].Payload), `"reason":"USER_CANCELLED"`) {
			t.Errorf("payload = %s", store.outbox[0].Payload)
		}
	}
}

func TestCancelTerminalOrders(t *testing.T) {
	for _, status := range []domain.Status{domain.Confirmed, domain.Rejected, domain.Cancelled} {
		store := newFakeStore()
		o := storedOrder(t, store, status, domain.Reserved)

		_, err := newTestService(store).CancelOrder(context.Background(), o.ID, "u1")
		if errorCode(err) != domain.CodeCannotCancel || len(store.outbox) != 0 {
			t.Errorf("%s: err=%v outbox=%d", status, err, len(store.outbox))
		}
	}
}

func TestCancelNotOwnerOrMissing(t *testing.T) {
	store := newFakeStore()
	o := storedOrder(t, store, domain.Pending, domain.Reserving)
	svc := newTestService(store)

	if _, err := svc.CancelOrder(context.Background(), o.ID, "u2"); errorCode(err) != domain.CodeNotFound {
		t.Errorf("other user: err=%v", err)
	}
	if _, err := svc.CancelOrder(context.Background(), "missing", "u1"); errorCode(err) != domain.CodeNotFound {
		t.Errorf("missing: err=%v", err)
	}
}

func TestCancelLosesRace(t *testing.T) {
	store := newFakeStore()
	o := storedOrder(t, store, domain.Pending, domain.Reserved)
	store.beforeSave = func(id string) { store.orders[id].Status = domain.Confirmed }

	_, err := newTestService(store).CancelOrder(context.Background(), o.ID, "u1")
	if errorCode(err) != domain.CodeCannotCancel {
		t.Errorf("err = %v", err)
	}
}

func TestCancelRollsBackWhenEnqueueFails(t *testing.T) {
	store := newFakeStore()
	o := storedOrder(t, store, domain.Pending, domain.Reserving)
	store.failEnqueue = errors.New("down")

	_, err := newTestService(store).CancelOrder(context.Background(), o.ID, "u1")
	if err == nil || store.orders[o.ID].Status != domain.Pending {
		t.Errorf("err=%v status=%s", err, store.orders[o.ID].Status)
	}
}
