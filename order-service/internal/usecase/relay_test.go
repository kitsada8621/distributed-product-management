package usecase

import (
	"context"
	"errors"
	"testing"
	"time"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain/event"
)

var now = time.Date(2026, 10, 10, 8, 30, 0, 0, time.UTC)

type fakeOutbox struct {
	due    []domain.OutboxEntry
	sent   []string
	failed map[string]failure
}

type failure struct {
	attempts int
	next     time.Time
}

func (f *fakeOutbox) FetchDue(context.Context, time.Time, int) ([]domain.OutboxEntry, error) {
	return f.due, nil
}

func (f *fakeOutbox) MarkSent(_ context.Context, id string, _ time.Time) error {
	f.sent = append(f.sent, id)
	return nil
}

func (f *fakeOutbox) MarkFailed(_ context.Context, id string, attempts int, next time.Time) error {
	if f.failed == nil {
		f.failed = map[string]failure{}
	}
	f.failed[id] = failure{attempts, next}
	return nil
}

type fakePublisher struct {
	err       error
	published []string
}

func (p *fakePublisher) Publish(_ context.Context, msg event.Message) error {
	if p.err != nil {
		return p.err
	}
	p.published = append(p.published, msg.ID)
	return nil
}

func entry(id string, attempts int) domain.OutboxEntry {
	return domain.OutboxEntry{Message: event.Message{ID: id, Topic: "order.created", Key: "o1"}, Attempts: attempts}
}

func newRelay(store *fakeOutbox, publisher *fakePublisher) *Relay {
	return NewRelay(store, publisher, func() time.Time { return now })
}

func TestRunOncePublishesInOrderAndMarksSent(t *testing.T) {
	store := &fakeOutbox{due: []domain.OutboxEntry{entry("a", 0), entry("b", 0)}}
	publisher := &fakePublisher{}

	if err := newRelay(store, publisher).RunOnce(context.Background()); err != nil {
		t.Fatal(err)
	}
	if len(publisher.published) != 2 || publisher.published[0] != "a" || publisher.published[1] != "b" {
		t.Errorf("published = %v", publisher.published)
	}
	if len(store.sent) != 2 || len(store.failed) != 0 {
		t.Errorf("sent=%v failed=%v", store.sent, store.failed)
	}
}

func TestRunOnceFailureBacksOffAndStopsTheTick(t *testing.T) {
	store := &fakeOutbox{due: []domain.OutboxEntry{entry("a", 0), entry("b", 0)}}
	publisher := &fakePublisher{err: errors.New("broker down")}

	if err := newRelay(store, publisher).RunOnce(context.Background()); err != nil {
		t.Fatal(err)
	}
	got, ok := store.failed["a"]
	if !ok || got.attempts != 1 || !got.next.Equal(now.Add(time.Second)) {
		t.Errorf("failed = %+v", store.failed)
	}
	if _, touched := store.failed["b"]; touched || len(store.sent) != 0 {
		t.Errorf("second row must wait: failed=%v sent=%v", store.failed, store.sent)
	}
}

func TestBackoffGrowsAndIsCapped(t *testing.T) {
	want := map[int]time.Duration{1: time.Second, 2: 2 * time.Second, 3: 4 * time.Second, 6: 32 * time.Second, 7: 60 * time.Second, 20: 60 * time.Second}
	for attempts, expected := range want {
		if got := backoff(attempts); got != expected {
			t.Errorf("backoff(%d) = %v, want %v", attempts, got, expected)
		}
	}
}

func TestRunOnceRetryCountsAttempts(t *testing.T) {
	store := &fakeOutbox{due: []domain.OutboxEntry{entry("a", 2)}}
	publisher := &fakePublisher{err: errors.New("down")}

	_ = newRelay(store, publisher).RunOnce(context.Background())
	if got := store.failed["a"]; got.attempts != 3 || !got.next.Equal(now.Add(4*time.Second)) {
		t.Errorf("failed = %+v", got)
	}
}

func TestRunOnceNothingDue(t *testing.T) {
	publisher := &fakePublisher{}
	if err := newRelay(&fakeOutbox{}, publisher).RunOnce(context.Background()); err != nil || len(publisher.published) != 0 {
		t.Errorf("err=%v published=%v", err, publisher.published)
	}
}
