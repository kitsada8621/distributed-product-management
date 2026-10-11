package repository

import (
	"context"
	"path/filepath"
	"testing"
	"time"

	"gorm.io/gorm"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/infrastructure/database"
)

var testNow = time.Date(2026, 10, 10, 8, 30, 0, 0, time.UTC)

var testCaller = domain.Caller{UserID: "u1", Email: "u1@example.com", Role: domain.RoleUser}

var testAddress = domain.ShippingAddress{
	RecipientName: "Somchai Jaidee",
	Phone:         "0812345678",
	AddressLine:   "99/9 Soi Sukhumvit 21, Khlong Toei Nuea, Watthana",
	Province:      "Bangkok",
	PostalCode:    "10110",
}

func openTestDB(t *testing.T) *gorm.DB {
	t.Helper()
	db, err := database.Open(filepath.Join(t.TempDir(), "order.db"))
	if err != nil {
		t.Fatal(err)
	}
	if err := database.Migrate(db); err != nil {
		t.Fatal(err)
	}

	t.Cleanup(func() {
		sqlDB, err := db.DB()
		if err == nil {
			_ = sqlDB.Close()
		}
	})
	return db
}

func TestStoreKeepsAddressAndPayment(t *testing.T) {
	store := NewStore(openTestDB(t))
	ctx := context.Background()

	o, err := domain.New(testCaller, []domain.ItemInput{{ProductID: "p1", Quantity: 1}}, testAddress, domain.PayAtStore, testNow)
	if err != nil {
		t.Fatal(err)
	}
	if err := store.Create(ctx, o, "key-1", "hash-1"); err != nil {
		t.Fatal(err)
	}

	got, err := store.GetByID(ctx, o.ID)
	if err != nil {
		t.Fatal(err)
	}
	if got.Address != testAddress || got.Payment != domain.PayAtStore {
		t.Errorf("address = %+v payment = %s", got.Address, got.Payment)
	}
}

func TestStoreRejectsUnknownPaymentMethod(t *testing.T) {
	db := openTestDB(t)

	err := db.Exec(`INSERT INTO orders (id, user_id, owner_email, recipient_name, phone, address_line, province, postal_code, payment_method, created_at, updated_at)
		VALUES ('o1', 'u1', 'u1@example.com', 'A', '0812345678', 'B', 'C', '10110', 'BITCOIN', '2026-10-10T08:30:00Z', '2026-10-10T08:30:00Z')`).Error
	if err == nil {
		t.Fatal("expected the CHECK on payment_method to reject BITCOIN")
	}
}
