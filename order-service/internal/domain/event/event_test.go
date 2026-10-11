package event

import (
	"encoding/json"
	"sort"
	"testing"
	"time"
)

var fixedNow = time.Date(2026, 10, 10, 8, 30, 0, 0, time.UTC)

func keysOf(t *testing.T, payload []byte) []string {
	t.Helper()
	var decoded map[string]any
	if err := json.Unmarshal(payload, &decoded); err != nil {
		t.Fatal(err)
	}
	var keys []string
	for key := range decoded {
		keys = append(keys, key)
	}
	sort.Strings(keys)
	return keys
}

func equalKeys(t *testing.T, got []string, want ...string) {
	t.Helper()
	sort.Strings(want)
	if len(got) != len(want) {
		t.Fatalf("keys = %v, want %v", got, want)
	}
	for i := range got {
		if got[i] != want[i] {
			t.Fatalf("keys = %v, want %v", got, want)
		}
	}
}

func TestOrderCreated(t *testing.T) {
	msg := NewOrderCreated("o1", "u1", []Item{{ProductID: "p1", Quantity: 2}}, "corr", fixedNow)
	if msg.Topic != TopicOrderCreated || msg.Key != "o1" || msg.CorrelationID != "corr" || msg.ID == "" {
		t.Errorf("message = %+v", msg)
	}
	equalKeys(t, keysOf(t, msg.Payload), "eventId", "occurredAt", "version", "orderId", "userId", "items")
	var decoded map[string]any
	_ = json.Unmarshal(msg.Payload, &decoded)
	if decoded["eventId"] != msg.ID || decoded["version"] != float64(1) || decoded["occurredAt"] != "2026-10-10T08:30:00Z" {
		t.Errorf("payload = %s", msg.Payload)
	}
}

func TestOrderConfirmedAndCancelled(t *testing.T) {
	confirmed := NewOrderConfirmed("o1", "c", fixedNow)
	equalKeys(t, keysOf(t, confirmed.Payload), "eventId", "occurredAt", "version", "orderId")
	cancelled := NewOrderCancelled("o1", ReasonTimeout, "c", fixedNow)
	equalKeys(t, keysOf(t, cancelled.Payload), "eventId", "occurredAt", "version", "orderId", "reason")
	if cancelled.Topic != TopicOrderCancelled || cancelled.Key != "o1" {
		t.Errorf("message = %+v", cancelled)
	}
}

func TestDeadLetter(t *testing.T) {
	msg := NewDeadLetter(TopicStockReserved, "o1", []byte(`{"x":1}`), "boom", 3, "c", fixedNow)
	if msg.Topic != "dlq.stock.reserved" || msg.Key != "o1" {
		t.Errorf("message = %+v", msg)
	}
	var decoded map[string]any
	_ = json.Unmarshal(msg.Payload, &decoded)
	if decoded["originalMessage"] != `{"x":1}` || decoded["error"] != "boom" || decoded["attempts"] != float64(3) || decoded["originalTopic"] != "stock.reserved" {
		t.Errorf("payload = %s", msg.Payload)
	}
}

func TestDecodeStockReserved(t *testing.T) {
	good := `{"eventId":"e1","orderId":"o1","version":1,"items":[{"productId":"p1","name":"Keyboard","unitPrice":1290.00,"quantity":2}]}`
	ev, err := DecodeStockReserved([]byte(good))
	if err != nil || ev.Items[0].UnitPrice != 1290 || ev.Items[0].Name != "Keyboard" {
		t.Fatalf("ev = %+v err = %v", ev, err)
	}
	for _, bad := range []string{`{`, `{"orderId":"o1","version":1}`, `{"eventId":"e","version":1}`, `{"eventId":"e","orderId":"o","version":2}`} {
		if _, err := DecodeStockReserved([]byte(bad)); err == nil {
			t.Errorf("want error for %s", bad)
		}
	}
}

func TestDecodeStockRejected(t *testing.T) {
	good := `{"eventId":"e1","orderId":"o1","version":1,"failedItems":[{"productId":"p1","reason":"INSUFFICIENT_STOCK","requested":5,"available":2}]}`
	ev, err := DecodeStockRejected([]byte(good))
	if err != nil || ev.FailedItems[0].Requested != 5 || ev.FailedItems[0].Available != 2 {
		t.Fatalf("ev = %+v err = %v", ev, err)
	}
	if _, err := DecodeStockRejected([]byte(`{"eventId":"e1","orderId":"o1","version":3}`)); err == nil {
		t.Error("want version error")
	}
}
