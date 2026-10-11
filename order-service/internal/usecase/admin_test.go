package usecase

import (
	"context"
	"strings"
	"testing"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain/event"
)

func TestConfirmReserved(t *testing.T) {
	store := newFakeStore()
	o := storedOrder(t, store, domain.Pending, domain.Reserved)

	got, err := newTestService(store).ConfirmOrder(context.Background(), o.ID)
	if err != nil || got.Status != domain.Confirmed || store.orders[o.ID].Status != domain.Confirmed {
		t.Fatalf("got=%+v err=%v", got, err)
	}
	if len(store.outbox) != 1 || store.outbox[0].Topic != event.TopicOrderConfirmed || store.outbox[0].Key != o.ID {
		t.Errorf("outbox = %+v", store.outbox)
	}
}

func TestConfirmWrongState(t *testing.T) {
	cases := []struct {
		status domain.Status
		stock  domain.StockStatus
	}{{domain.Pending, domain.Reserving}, {domain.Cancelled, domain.Reserved}, {domain.Confirmed, domain.Reserved}, {domain.Rejected, domain.Reserving}}
	for _, tc := range cases {
		store := newFakeStore()
		o := storedOrder(t, store, tc.status, tc.stock)

		_, err := newTestService(store).ConfirmOrder(context.Background(), o.ID)
		if errorCode(err) != domain.CodeInvalidState || len(store.outbox) != 0 {
			t.Errorf("%s/%s: err=%v outbox=%d", tc.status, tc.stock, err, len(store.outbox))
		}
	}
}

func TestConfirmUnknownOrder(t *testing.T) {
	_, err := newTestService(newFakeStore()).ConfirmOrder(context.Background(), "missing")
	if errorCode(err) != domain.CodeNotFound {
		t.Errorf("err = %v", err)
	}
}

func TestConfirmActsOnAnyUsersOrder(t *testing.T) {
	store := newFakeStore()
	o := storedOrder(t, store, domain.Pending, domain.Reserved)
	if store.orders[o.ID].UserID == "admin" {
		t.Fatal("setup: order must belong to someone else")
	}
	if _, err := newTestService(store).ConfirmOrder(context.Background(), o.ID); err != nil {
		t.Errorf("err = %v", err)
	}
}

func TestRejectReserved(t *testing.T) {
	store := newFakeStore()
	o := storedOrder(t, store, domain.Pending, domain.Reserved)

	got, err := newTestService(store).RejectOrder(context.Background(), o.ID, "Item damaged")
	if err != nil || got.Status != domain.Rejected {
		t.Fatalf("got=%+v err=%v", got, err)
	}
	if got.Rejection.Type != domain.AdminRejected || *got.Rejection.Message != "Item damaged" {
		t.Errorf("rejection = %+v", got.Rejection)
	}
	if len(store.outbox) != 1 || store.outbox[0].Topic != event.TopicOrderCancelled ||
		!strings.Contains(string(store.outbox[0].Payload), `"reason":"ADMIN_REJECTED"`) {
		t.Errorf("outbox = %+v", store.outbox)
	}
}

func TestRejectErrors(t *testing.T) {
	store := newFakeStore()
	reserved := storedOrder(t, store, domain.Pending, domain.Reserved)
	svc := newTestService(store)

	if _, err := svc.RejectOrder(context.Background(), reserved.ID, " "); errorCode(err) != domain.CodeValidation {
		t.Errorf("blank reason: err=%v", err)
	}
	if _, err := svc.RejectOrder(context.Background(), "missing", "x"); errorCode(err) != domain.CodeNotFound {
		t.Errorf("missing: err=%v", err)
	}

	reserving := storedOrder(t, newFakeStore(), domain.Pending, domain.Reserving)
	other := newFakeStore()
	other.orders[reserving.ID] = reserving
	if _, err := newTestService(other).RejectOrder(context.Background(), reserving.ID, "x"); errorCode(err) != domain.CodeInvalidState {
		t.Errorf("reserving: err=%v", err)
	}
}

func TestDecisionLosesRace(t *testing.T) {
	store := newFakeStore()
	o := storedOrder(t, store, domain.Pending, domain.Reserved)
	store.beforeSave = func(id string) { store.orders[id].Status = domain.Cancelled }

	_, err := newTestService(store).ConfirmOrder(context.Background(), o.ID)
	if errorCode(err) != domain.CodeInvalidState {
		t.Errorf("err = %v", err)
	}
}
