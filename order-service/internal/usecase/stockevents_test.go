package usecase

import (
	"context"
	"testing"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain/event"
)

func reservedEvent(eventID, orderID string, price float64) event.StockReserved {
	return event.StockReserved{
		EventID: eventID, OrderID: orderID, Version: 1,
		Items: []event.ReservedItem{{ProductID: "p1", Name: "Keyboard", UnitPrice: price, Quantity: 2}},
	}
}

func TestHandleStockReserved(t *testing.T) {
	store := newFakeStore()
	o := storedOrder(t, store, domain.Pending, domain.Reserving)
	store.orders[o.ID].Items = []domain.Item{{ID: "i1", ProductID: "p1", Quantity: 2}}

	if err := newTestService(store).HandleStockReserved(context.Background(), reservedEvent("e1", o.ID, 1290.00)); err != nil {
		t.Fatal(err)
	}
	got := store.orders[o.ID]
	if got.StockStatus != domain.Reserved || *got.Items[0].Name != "Keyboard" || *got.Items[0].UnitPriceMinor != 129000 || *got.TotalMinor != 258000 {
		t.Errorf("order = %+v", got)
	}
}

func TestHandleStockReservedDuplicateEvent(t *testing.T) {
	store := newFakeStore()
	o := storedOrder(t, store, domain.Pending, domain.Reserving)
	store.orders[o.ID].Items = []domain.Item{{ID: "i1", ProductID: "p1", Quantity: 2}}
	svc := newTestService(store)

	_ = svc.HandleStockReserved(context.Background(), reservedEvent("e1", o.ID, 10))
	store.orders[o.ID].StockStatus = domain.Reserving
	if err := svc.HandleStockReserved(context.Background(), reservedEvent("e1", o.ID, 99)); err != nil {
		t.Fatal(err)
	}
	if store.orders[o.ID].StockStatus != domain.Reserving {
		t.Error("duplicate event was applied")
	}
}

func TestHandleStockReservedLateEventIsSkipped(t *testing.T) {
	store := newFakeStore()
	o := storedOrder(t, store, domain.Cancelled, domain.Reserving)
	store.orders[o.ID].Items = []domain.Item{{ID: "i1", ProductID: "p1", Quantity: 2}}

	if err := newTestService(store).HandleStockReserved(context.Background(), reservedEvent("e1", o.ID, 10)); err != nil {
		t.Fatal(err)
	}
	if store.orders[o.ID].Status != domain.Cancelled || store.orders[o.ID].Items[0].Name != nil || !store.processed["e1"] {
		t.Errorf("order = %+v processed = %v", store.orders[o.ID], store.processed)
	}
}

func TestHandleStockReservedErrorsKeepNoInboxRow(t *testing.T) {
	store := newFakeStore()
	svc := newTestService(store)
	if err := svc.HandleStockReserved(context.Background(), reservedEvent("e1", "missing", 10)); err == nil {
		t.Error("want error for unknown order")
	}

	o := storedOrder(t, store, domain.Pending, domain.Reserving)
	mismatch := reservedEvent("e2", o.ID, 10)
	mismatch.Items[0].ProductID = "other"
	if err := svc.HandleStockReserved(context.Background(), mismatch); err == nil {
		t.Error("want error for item mismatch")
	}
	if store.processed["e1"] || store.processed["e2"] {
		t.Errorf("inbox rows kept: %v", store.processed)
	}
}

func rejectedEvent(eventID, orderID string) event.StockRejected {
	return event.StockRejected{
		EventID: eventID, OrderID: orderID, Version: 1,
		FailedItems: []event.FailedItem{{ProductID: "p1", Reason: "INSUFFICIENT_STOCK", Requested: 5, Available: 2}},
	}
}

func TestHandleStockRejected(t *testing.T) {
	store := newFakeStore()
	o := storedOrder(t, store, domain.Pending, domain.Reserving)
	store.orders[o.ID].Items = []domain.Item{{ID: "i1", ProductID: "p1", Quantity: 5}}

	if err := newTestService(store).HandleStockRejected(context.Background(), rejectedEvent("e1", o.ID)); err != nil {
		t.Fatal(err)
	}
	got := store.orders[o.ID]
	failure := got.Items[0].Failure
	if got.Status != domain.Rejected || got.Rejection.Type != domain.StockUnavailable || failure.Requested != 5 || failure.Available != 2 {
		t.Errorf("order = %+v", got)
	}
}

func TestHandleStockRejectedSkipsAndDeduplicates(t *testing.T) {
	store := newFakeStore()
	o := storedOrder(t, store, domain.Cancelled, domain.Reserving)
	store.orders[o.ID].Items = []domain.Item{{ID: "i1", ProductID: "p1", Quantity: 5}}
	svc := newTestService(store)

	if err := svc.HandleStockRejected(context.Background(), rejectedEvent("e1", o.ID)); err != nil {
		t.Fatal(err)
	}
	if store.orders[o.ID].Status != domain.Cancelled {
		t.Errorf("status = %s", store.orders[o.ID].Status)
	}
	if err := svc.HandleStockRejected(context.Background(), rejectedEvent("e1", o.ID)); err != nil {
		t.Fatal(err)
	}
}
