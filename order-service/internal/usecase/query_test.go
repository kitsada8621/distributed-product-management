package usecase

import (
	"context"
	"fmt"
	"testing"
	"time"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain"
)

func seedOrders(t *testing.T, store *fakeStore, userID string, count int) []*domain.Order {
	t.Helper()
	var created []*domain.Order
	for i := 0; i < count; i++ {
		caller := domain.Caller{UserID: userID, Email: userID + "@example.com", Role: domain.RoleUser}
		o, err := domain.New(caller, []domain.ItemInput{{ProductID: "p1", Quantity: 1}}, testAddress, domain.CashOnDelivery, testNow.Add(time.Duration(len(store.orders))*time.Second))
		if err != nil {
			t.Fatal(err)
		}
		if err := store.Create(context.Background(), o, fmt.Sprintf("%s-%d-%d", userID, i, len(store.orders)), "h"); err != nil {
			t.Fatal(err)
		}
		created = append(created, o)
	}
	return created
}

func TestListMineOnlyOwnNewestFirst(t *testing.T) {
	store := newFakeStore()
	mine := seedOrders(t, store, "u1", 3)
	seedOrders(t, store, "u2", 2)
	svc := newTestService(store)

	page, err := svc.ListMine(context.Background(), "u1", domain.ListFilter{Page: 1, PageSize: 20})
	if err != nil || page.Total != 3 || len(page.Rows) != 3 {
		t.Fatalf("page=%+v err=%v", page, err)
	}
	if page.Rows[0].ID != mine[2].ID || page.Rows[2].ID != mine[0].ID {
		t.Errorf("rows not newest first: %+v", page.Rows)
	}
}

func TestListPaging(t *testing.T) {
	store := newFakeStore()
	seedOrders(t, store, "u1", 25)
	svc := newTestService(store)

	first, _ := svc.ListMine(context.Background(), "u1", domain.ListFilter{Page: 1, PageSize: 20})
	second, _ := svc.ListMine(context.Background(), "u1", domain.ListFilter{Page: 2, PageSize: 20})
	beyond, _ := svc.ListMine(context.Background(), "u1", domain.ListFilter{Page: 9, PageSize: 20})
	if len(first.Rows) != 20 || len(second.Rows) != 5 || len(beyond.Rows) != 0 || beyond.Total != 25 {
		t.Errorf("first=%d second=%d beyond=%d total=%d", len(first.Rows), len(second.Rows), len(beyond.Rows), beyond.Total)
	}
}

func TestListAllAndStatusFilter(t *testing.T) {
	store := newFakeStore()
	created := seedOrders(t, store, "u1", 2)
	seedOrders(t, store, "u2", 1)
	store.orders[created[0].ID].Status = domain.Cancelled
	svc := newTestService(store)

	all, _ := svc.ListAll(context.Background(), domain.ListFilter{Page: 1, PageSize: 20})
	if all.Total != 3 || all.Rows[0].OwnerEmail == "" {
		t.Errorf("all = %+v", all)
	}

	cancelled := domain.Cancelled
	filtered, _ := svc.ListAll(context.Background(), domain.ListFilter{Status: &cancelled, Page: 1, PageSize: 20})
	if filtered.Total != 1 || filtered.Rows[0].ID != created[0].ID {
		t.Errorf("filtered = %+v", filtered)
	}
}

func TestGetMineAndGetAny(t *testing.T) {
	store := newFakeStore()
	mine := seedOrders(t, store, "u1", 1)[0]
	svc := newTestService(store)

	if o, err := svc.GetMine(context.Background(), mine.ID, "u1"); err != nil || o.ID != mine.ID {
		t.Errorf("owner: o=%v err=%v", o, err)
	}
	if _, err := svc.GetMine(context.Background(), mine.ID, "u2"); errorCode(err) != domain.CodeNotFound {
		t.Errorf("other user: err=%v", err)
	}
	if _, err := svc.GetMine(context.Background(), "missing", "u1"); errorCode(err) != domain.CodeNotFound {
		t.Errorf("missing: err=%v", err)
	}
	if o, err := svc.GetAny(context.Background(), mine.ID); err != nil || o.ID != mine.ID {
		t.Errorf("admin: o=%v err=%v", o, err)
	}
	if _, err := svc.GetAny(context.Background(), "missing"); errorCode(err) != domain.CodeNotFound {
		t.Errorf("admin missing: err=%v", err)
	}
}

func TestParseStatus(t *testing.T) {
	tests := []struct {
		value   string
		want    *domain.Status
		wantErr bool
	}{
		{"", nil, false},
		{"Pending", ptr(domain.Pending), false},
		{"Cancelled", ptr(domain.Cancelled), false},
		{"pending", nil, true},
		{"Foo", nil, true},
	}
	for _, tt := range tests {
		got, err := ParseStatus(tt.value)
		if (err != nil) != tt.wantErr {
			t.Errorf("%q: err = %v", tt.value, err)
		}
		if (got == nil) != (tt.want == nil) || (got != nil && *got != *tt.want) {
			t.Errorf("%q: got %v want %v", tt.value, got, tt.want)
		}
	}
}

func ptr[T any](v T) *T { return &v }
