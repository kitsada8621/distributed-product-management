package usecase

import (
	"context"
	"encoding/json"
	"errors"
	"testing"
	"time"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain/event"
	"github.com/kitsada8621/distributed-product-management/order-service/pkg/correlation"
)

func newTestService(store *fakeStore) *Service {
	return NewService(store, func() time.Time { return testNow })
}

func TestCreateOrderNew(t *testing.T) {
	store := newFakeStore()
	svc := newTestService(store)
	ctx := correlation.With(context.Background(), "corr-1")

	result, err := svc.CreateOrder(ctx, testCaller, "key-1", []domain.ItemInput{{ProductID: "p1", Quantity: 2}, {ProductID: "p2", Quantity: 1}}, testAddress, domain.CashOnDelivery)
	if err != nil || !result.Created {
		t.Fatalf("result=%+v err=%v", result, err)
	}
	if len(store.orders) != 1 || len(store.outbox) != 1 {
		t.Fatalf("orders=%d outbox=%d", len(store.orders), len(store.outbox))
	}

	msg := store.outbox[0]
	if msg.Topic != event.TopicOrderCreated || msg.Key != result.Order.ID || msg.CorrelationID != "corr-1" {
		t.Errorf("message = %+v", msg)
	}
	var payload struct {
		UserID string
		Items  []struct {
			ProductID string
			Quantity  int
		}
	}
	_ = json.Unmarshal(msg.Payload, &payload)
	if payload.UserID != "u1" || len(payload.Items) != 2 || payload.Items[0].ProductID != "p1" || payload.Items[0].Quantity != 2 {
		t.Errorf("payload = %s", msg.Payload)
	}
}

func TestCreateOrderRepeatedKey(t *testing.T) {
	store := newFakeStore()
	svc := newTestService(store)
	first, _ := svc.CreateOrder(context.Background(), testCaller, "key-1", []domain.ItemInput{{ProductID: "p1", Quantity: 2}, {ProductID: "p2", Quantity: 1}}, testAddress, domain.CashOnDelivery)

	again, err := svc.CreateOrder(context.Background(), testCaller, "key-1", []domain.ItemInput{{ProductID: "p2", Quantity: 1}, {ProductID: "p1", Quantity: 2}}, testAddress, domain.CashOnDelivery)
	if err != nil || again.Created || again.Order.ID != first.Order.ID {
		t.Fatalf("again=%+v err=%v", again, err)
	}
	if len(store.orders) != 1 || len(store.outbox) != 1 {
		t.Errorf("orders=%d outbox=%d", len(store.orders), len(store.outbox))
	}
}

func TestCreateOrderReusedKeyWithOtherBody(t *testing.T) {
	store := newFakeStore()
	svc := newTestService(store)
	_, _ = svc.CreateOrder(context.Background(), testCaller, "key-1", []domain.ItemInput{{ProductID: "p1", Quantity: 2}}, testAddress, domain.CashOnDelivery)

	_, err := svc.CreateOrder(context.Background(), testCaller, "key-1", []domain.ItemInput{{ProductID: "p1", Quantity: 3}}, testAddress, domain.CashOnDelivery)
	if errorCode(err) != domain.CodeIdempotencyKeyReused {
		t.Errorf("err = %v", err)
	}
	if len(store.orders) != 1 {
		t.Errorf("orders = %d", len(store.orders))
	}
}

func TestCreateOrderBlankKey(t *testing.T) {
	_, err := newTestService(newFakeStore()).CreateOrder(context.Background(), testCaller, "  ", []domain.ItemInput{{ProductID: "p1", Quantity: 1}}, testAddress, domain.CashOnDelivery)
	if errorCode(err) != domain.CodeIdempotencyKeyRequired {
		t.Errorf("err = %v", err)
	}
}

func TestCreateOrderInvalidInputCreatesNothing(t *testing.T) {
	store := newFakeStore()
	_, err := newTestService(store).CreateOrder(context.Background(), testCaller, "k", []domain.ItemInput{{ProductID: "p1", Quantity: 0}}, testAddress, domain.CashOnDelivery)
	if errorCode(err) != domain.CodeValidation || len(store.orders) != 0 || len(store.outbox) != 0 {
		t.Errorf("err=%v orders=%d outbox=%d", err, len(store.orders), len(store.outbox))
	}
}

func TestCreateOrderSameKeyOtherUser(t *testing.T) {
	store := newFakeStore()
	svc := newTestService(store)
	_, _ = svc.CreateOrder(context.Background(), testCaller, "key-1", []domain.ItemInput{{ProductID: "p1", Quantity: 1}}, testAddress, domain.CashOnDelivery)
	other := domain.Caller{UserID: "u2", Email: "u2@example.com", Role: domain.RoleUser}

	result, err := svc.CreateOrder(context.Background(), other, "key-1", []domain.ItemInput{{ProductID: "p1", Quantity: 1}}, testAddress, domain.CashOnDelivery)
	if err != nil || !result.Created || len(store.orders) != 2 {
		t.Errorf("result=%+v err=%v orders=%d", result, err, len(store.orders))
	}
}

func TestCreateOrderRollsBackWhenEnqueueFails(t *testing.T) {
	store := newFakeStore()
	store.failEnqueue = errors.New("outbox down")

	_, err := newTestService(store).CreateOrder(context.Background(), testCaller, "k", []domain.ItemInput{{ProductID: "p1", Quantity: 1}}, testAddress, domain.CashOnDelivery)
	if err == nil || len(store.orders) != 0 || len(store.idempotency) != 0 {
		t.Errorf("err=%v orders=%d idempotency=%d", err, len(store.orders), len(store.idempotency))
	}
}

func TestCreateOrderStoresAddressAndPayment(t *testing.T) {
	store := newFakeStore()

	result, err := newTestService(store).CreateOrder(context.Background(), testCaller, "key-1", []domain.ItemInput{{ProductID: "p1", Quantity: 1}}, testAddress, domain.PayAtStore)
	if err != nil {
		t.Fatal(err)
	}
	stored := store.orders[result.Order.ID]
	if stored.Address != testAddress || stored.Payment != domain.PayAtStore {
		t.Errorf("stored address = %+v payment = %s", stored.Address, stored.Payment)
	}
}

func TestCreateOrderReusedKeyWithOtherAddressOrPayment(t *testing.T) {
	otherAddress := testAddress
	otherAddress.AddressLine = "1 Another Road"
	tests := []struct {
		name    string
		address domain.ShippingAddress
		payment domain.PaymentMethod
	}{
		{"other address", otherAddress, domain.CashOnDelivery},
		{"other payment", testAddress, domain.PayAtStore},
	}
	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			svc := newTestService(newFakeStore())
			inputs := []domain.ItemInput{{ProductID: "p1", Quantity: 1}}
			_, _ = svc.CreateOrder(context.Background(), testCaller, "key-1", inputs, testAddress, domain.CashOnDelivery)

			_, err := svc.CreateOrder(context.Background(), testCaller, "key-1", inputs, tt.address, tt.payment)
			if errorCode(err) != domain.CodeIdempotencyKeyReused {
				t.Errorf("err = %v", err)
			}
		})
	}
}

func TestCreateOrderSameBodyWithExtraSpacesIsSameRequest(t *testing.T) {
	svc := newTestService(newFakeStore())
	inputs := []domain.ItemInput{{ProductID: "p1", Quantity: 1}}
	_, _ = svc.CreateOrder(context.Background(), testCaller, "key-1", inputs, testAddress, domain.CashOnDelivery)

	padded := testAddress
	padded.Province = "  Bangkok "
	again, err := svc.CreateOrder(context.Background(), testCaller, "key-1", inputs, padded, domain.CashOnDelivery)
	if err != nil || again.Created {
		t.Errorf("again=%+v err=%v", again, err)
	}
}

func TestCreateOrderEventHasNoAddressOrPayment(t *testing.T) {
	store := newFakeStore()
	_, err := newTestService(store).CreateOrder(context.Background(), testCaller, "key-1", []domain.ItemInput{{ProductID: "p1", Quantity: 1}}, testAddress, domain.CashOnDelivery)
	if err != nil {
		t.Fatal(err)
	}

	var payload map[string]any
	_ = json.Unmarshal(store.outbox[0].Payload, &payload)
	if _, found := payload["shippingAddress"]; found {
		t.Errorf("payload has shippingAddress: %s", store.outbox[0].Payload)
	}
	if _, found := payload["paymentMethod"]; found {
		t.Errorf("payload has paymentMethod: %s", store.outbox[0].Payload)
	}
}
