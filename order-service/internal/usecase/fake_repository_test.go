package usecase

import (
	"context"
	"errors"
	"sort"
	"testing"
	"time"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain/event"
)

type fakeStore struct {
	orders      map[string]*domain.Order
	idempotency map[string]idempotencyEntry
	outbox      []event.Message
	processed   map[string]bool

	failEnqueue error

	beforeSave func(id string)
}

type idempotencyEntry struct {
	hash    string
	orderID string
}

func newFakeStore() *fakeStore {
	return &fakeStore{
		orders:      map[string]*domain.Order{},
		idempotency: map[string]idempotencyEntry{},
		processed:   map[string]bool{},
	}
}

func cloneOrder(o *domain.Order) *domain.Order {
	copied := *o
	copied.Items = append([]domain.Item(nil), o.Items...)
	return &copied
}

func (f *fakeStore) snapshot() *fakeStore {
	copied := newFakeStore()
	for id, o := range f.orders {
		copied.orders[id] = cloneOrder(o)
	}
	for key, entry := range f.idempotency {
		copied.idempotency[key] = entry
	}
	for id, done := range f.processed {
		copied.processed[id] = done
	}
	copied.outbox = append([]event.Message(nil), f.outbox...)
	return copied
}

func (f *fakeStore) restore(from *fakeStore) {
	f.orders, f.idempotency, f.processed, f.outbox = from.orders, from.idempotency, from.processed, from.outbox
}

func (f *fakeStore) WithinTx(ctx context.Context, fn func(ctx context.Context, tx domain.Repository) error) error {
	before := f.snapshot()
	if err := fn(ctx, f); err != nil {
		f.restore(before)
		return err
	}
	return nil
}

func idemKey(userID, key string) string { return userID + "|" + key }

func (f *fakeStore) FindIdempotency(_ context.Context, userID, key string) (*domain.IdempotencyRecord, error) {
	entry, ok := f.idempotency[idemKey(userID, key)]
	if !ok {
		return nil, nil
	}
	return &domain.IdempotencyRecord{RequestHash: entry.hash, Order: cloneOrder(f.orders[entry.orderID])}, nil
}

func (f *fakeStore) Create(_ context.Context, o *domain.Order, key, requestHash string) error {
	k := idemKey(o.UserID, key)
	if _, exists := f.idempotency[k]; exists {
		return domain.ErrDuplicateKey
	}
	f.orders[o.ID] = cloneOrder(o)
	f.idempotency[k] = idempotencyEntry{hash: requestHash, orderID: o.ID}
	return nil
}

func (f *fakeStore) GetByID(_ context.Context, id string) (*domain.Order, error) {
	o, ok := f.orders[id]
	if !ok {
		return nil, domain.ErrNotFound
	}
	return cloneOrder(o), nil
}

func (f *fakeStore) GetOwned(ctx context.Context, id, userID string) (*domain.Order, error) {
	o, err := f.GetByID(ctx, id)
	if err != nil {
		return nil, err
	}
	if o.UserID != userID {
		return nil, domain.ErrNotFound
	}
	return o, nil
}

func (f *fakeStore) Save(_ context.Context, o *domain.Order, before domain.State) error {
	if f.beforeSave != nil {
		f.beforeSave(o.ID)
	}
	stored, ok := f.orders[o.ID]
	if !ok {
		return domain.ErrNotFound
	}
	if stored.State() != before {
		return domain.ErrStateChanged
	}
	f.orders[o.ID] = cloneOrder(o)
	return nil
}

func (f *fakeStore) FindStale(_ context.Context, createdBefore time.Time, limit int) ([]*domain.Order, error) {
	var stale []*domain.Order
	for _, o := range f.orders {
		if o.Status == domain.Pending && o.StockStatus == domain.Reserving && o.CreatedAt.Before(createdBefore) {
			stale = append(stale, cloneOrder(o))
		}
	}
	sort.Slice(stale, func(i, j int) bool { return stale[i].CreatedAt.Before(stale[j].CreatedAt) })
	if len(stale) > limit {
		stale = stale[:limit]
	}
	return stale, nil
}

func (f *fakeStore) page(match func(*domain.Order) bool, filter domain.ListFilter) domain.OrderPage {
	var rows []domain.OrderRow
	for _, o := range f.orders {
		if !match(o) || (filter.Status != nil && o.Status != *filter.Status) {
			continue
		}
		rows = append(rows, domain.OrderRow{
			ID: o.ID, OwnerEmail: o.OwnerEmail, Status: o.Status, StockStatus: o.StockStatus,
			TotalMinor: o.TotalMinor, CreatedAt: o.CreatedAt,
		})
	}
	sort.Slice(rows, func(i, j int) bool { return rows[i].CreatedAt.After(rows[j].CreatedAt) })

	total := len(rows)
	start := (filter.Page - 1) * filter.PageSize
	if start > total {
		start = total
	}
	end := start + filter.PageSize
	if end > total {
		end = total
	}
	return domain.OrderPage{Rows: rows[start:end], Total: total}
}

func (f *fakeStore) ListByUser(_ context.Context, userID string, filter domain.ListFilter) (domain.OrderPage, error) {
	return f.page(func(o *domain.Order) bool { return o.UserID == userID }, filter), nil
}

func (f *fakeStore) ListAll(_ context.Context, filter domain.ListFilter) (domain.OrderPage, error) {
	return f.page(func(*domain.Order) bool { return true }, filter), nil
}

func (f *fakeStore) Enqueue(_ context.Context, msg event.Message) error {
	if f.failEnqueue != nil {
		return f.failEnqueue
	}
	f.outbox = append(f.outbox, msg)
	return nil
}

func (f *fakeStore) MarkProcessed(_ context.Context, eventID, _ string) (bool, error) {
	if f.processed[eventID] {
		return false, nil
	}
	f.processed[eventID] = true
	return true, nil
}

var _ domain.Store = (*fakeStore)(nil)

func TestFakeStoreRollback(t *testing.T) {
	store := newFakeStore()
	o, _ := domain.New(testCaller, []domain.ItemInput{{ProductID: "p1", Quantity: 1}}, testAddress, domain.CashOnDelivery, testNow)
	boom := errors.New("boom")

	err := store.WithinTx(context.Background(), func(ctx context.Context, tx domain.Repository) error {
		_ = tx.Create(ctx, o, "k", "h")
		_ = tx.Enqueue(ctx, event.NewOrderCreated(o.ID, "u1", nil, "", testNow))
		_, _ = tx.MarkProcessed(ctx, "e1", "t")
		return boom
	})
	if !errors.Is(err, boom) {
		t.Fatalf("err = %v", err)
	}
	if len(store.orders) != 0 || len(store.outbox) != 0 || len(store.processed) != 0 || len(store.idempotency) != 0 {
		t.Errorf("rollback incomplete: %+v", store)
	}
}

func TestFakeStoreSaveStateChanged(t *testing.T) {
	store := newFakeStore()
	o, _ := domain.New(testCaller, []domain.ItemInput{{ProductID: "p1", Quantity: 1}}, testAddress, domain.CashOnDelivery, testNow)
	_ = store.Create(context.Background(), o, "k", "h")

	changed := cloneOrder(o)
	changed.Status = domain.Cancelled
	if err := store.Save(context.Background(), changed, domain.State{Status: domain.Confirmed, StockStatus: domain.Reserving}); !errors.Is(err, domain.ErrStateChanged) {
		t.Fatalf("err = %v", err)
	}
	if err := store.Save(context.Background(), changed, o.State()); err != nil {
		t.Fatalf("err = %v", err)
	}
}

func errorCode(err error) string {
	var apiErr *domain.Error
	if errors.As(err, &apiErr) {
		return apiErr.Code
	}
	return ""
}

var testNow = time.Date(2026, 10, 10, 8, 30, 0, 0, time.UTC)

var testCaller = domain.Caller{UserID: "u1", Email: "u1@example.com", Role: domain.RoleUser}

var testAddress = domain.ShippingAddress{
	RecipientName: "Somchai Jaidee",
	Phone:         "0812345678",
	AddressLine:   "99/9 Soi Sukhumvit 21, Khlong Toei Nuea, Watthana",
	Province:      "Bangkok",
	PostalCode:    "10110",
}
