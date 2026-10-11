package domain

import (
	"errors"
	"strings"
	"testing"
	"time"
)

var testNow = time.Date(2026, 10, 10, 8, 30, 0, 0, time.UTC)

var testCaller = Caller{UserID: "u1", Email: "u1@example.com", Role: RoleUser}

var testAddress = ShippingAddress{
	RecipientName: "Somchai Jaidee",
	Phone:         "0812345678",
	AddressLine:   "99/9 Soi Sukhumvit 21, Khlong Toei Nuea, Watthana",
	Province:      "Bangkok",
	PostalCode:    "10110",
}

func TestNewValidOrder(t *testing.T) {
	o, err := New(testCaller, []ItemInput{{"p1", 2}, {"p2", 1}}, testAddress, CashOnDelivery, testNow)
	if err != nil {
		t.Fatal(err)
	}
	if o.Address != testAddress || o.Payment != CashOnDelivery {
		t.Errorf("address = %+v payment = %s", o.Address, o.Payment)
	}
	if o.Status != Pending || o.StockStatus != Reserving || o.UserID != "u1" || o.OwnerEmail != "u1@example.com" {
		t.Errorf("order = %+v", o)
	}
	if len(o.Items) != 2 || o.Items[0].ProductID != "p1" || o.Items[0].Quantity != 2 || o.Items[0].Name != nil {
		t.Errorf("items = %+v", o.Items)
	}
	if o.ID == "" || o.Items[0].ID == "" || !o.CreatedAt.Equal(testNow) || !o.UpdatedAt.Equal(testNow) {
		t.Errorf("ids/times wrong: %+v", o)
	}
}

func TestNewValidation(t *testing.T) {
	tests := []struct {
		name   string
		inputs []ItemInput
		want   []FieldError
	}{
		{"empty", nil, []FieldError{{"items", "REQUIRED", "At least one item is required"}}},
		{"blank product", []ItemInput{{"", 1}}, []FieldError{{"items[0].productId", "REQUIRED", "productId is required"}}},
		{"duplicate", []ItemInput{{"p1", 1}, {"p1", 2}}, []FieldError{{"items[1].productId", "DUPLICATE", "productId is repeated"}}},
		{"zero", []ItemInput{{"p1", 0}}, []FieldError{{"items[0].quantity", "OUT_OF_RANGE", "quantity must be greater than 0"}}},
		{"negative", []ItemInput{{"p1", -1}}, []FieldError{{"items[0].quantity", "OUT_OF_RANGE", "quantity must be greater than 0"}}},
		{"fraction", []ItemInput{{"p1", 1.5}}, []FieldError{{"items[0].quantity", "INVALID_FORMAT", "quantity must be an integer"}}},
		{"several", []ItemInput{{"p1", 0}, {"", 2}}, []FieldError{
			{"items[0].quantity", "OUT_OF_RANGE", "quantity must be greater than 0"},
			{"items[1].productId", "REQUIRED", "productId is required"},
		}},
	}
	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			o, err := New(testCaller, tt.inputs, testAddress, CashOnDelivery, testNow)
			var apiErr *Error
			if o != nil || !errors.As(err, &apiErr) || apiErr.Code != CodeValidation {
				t.Fatalf("o = %v err = %v", o, err)
			}
			if len(apiErr.Fields) != len(tt.want) {
				t.Fatalf("fields = %+v, want %+v", apiErr.Fields, tt.want)
			}
			for i := range tt.want {
				if apiErr.Fields[i] != tt.want[i] {
					t.Errorf("field %d = %+v, want %+v", i, apiErr.Fields[i], tt.want[i])
				}
			}
		})
	}
}

func TestNewTrimsAddressAndAcceptsPayAtStore(t *testing.T) {
	padded := testAddress
	padded.RecipientName = "  Somchai Jaidee "
	padded.PostalCode = " 10110 "

	o, err := New(testCaller, []ItemInput{{"p1", 1}}, padded, PayAtStore, testNow)
	if err != nil {
		t.Fatal(err)
	}
	if o.Address != testAddress || o.Payment != PayAtStore {
		t.Errorf("address = %+v payment = %s", o.Address, o.Payment)
	}
}

func TestNewAddressAndPaymentValidation(t *testing.T) {
	long := func(n int) string { return strings.Repeat("a", n) }
	required := func(field string) FieldError {
		return FieldError{field, "REQUIRED", field + " is required"}
	}
	phoneFormat := FieldError{"shippingAddress.phone", "INVALID_FORMAT", "phone must be 9-10 digits starting with 0"}
	postalFormat := FieldError{"shippingAddress.postalCode", "INVALID_FORMAT", "postalCode must be 5 digits"}

	tests := []struct {
		name    string
		change  func(a *ShippingAddress)
		payment PaymentMethod
		want    FieldError
	}{
		{"blank recipient", func(a *ShippingAddress) { a.RecipientName = "   " }, CashOnDelivery, required("shippingAddress.recipientName")},
		{"blank phone", func(a *ShippingAddress) { a.Phone = "" }, CashOnDelivery, required("shippingAddress.phone")},
		{"blank address line", func(a *ShippingAddress) { a.AddressLine = "" }, CashOnDelivery, required("shippingAddress.addressLine")},
		{"blank province", func(a *ShippingAddress) { a.Province = "" }, CashOnDelivery, required("shippingAddress.province")},
		{"blank postal code", func(a *ShippingAddress) { a.PostalCode = "" }, CashOnDelivery, required("shippingAddress.postalCode")},
		{"recipient too long", func(a *ShippingAddress) { a.RecipientName = long(101) }, CashOnDelivery,
			FieldError{"shippingAddress.recipientName", "TOO_LONG", "shippingAddress.recipientName must be at most 100 characters"}},
		{"address line too long", func(a *ShippingAddress) { a.AddressLine = long(256) }, CashOnDelivery,
			FieldError{"shippingAddress.addressLine", "TOO_LONG", "shippingAddress.addressLine must be at most 255 characters"}},
		{"province too long", func(a *ShippingAddress) { a.Province = long(101) }, CashOnDelivery,
			FieldError{"shippingAddress.province", "TOO_LONG", "shippingAddress.province must be at most 100 characters"}},
		{"phone without leading 0", func(a *ShippingAddress) { a.Phone = "812345678" }, CashOnDelivery, phoneFormat},
		{"phone too short", func(a *ShippingAddress) { a.Phone = "12345" }, CashOnDelivery, phoneFormat},
		{"postal code with letter", func(a *ShippingAddress) { a.PostalCode = "1011a" }, CashOnDelivery, postalFormat},
		{"postal code too short", func(a *ShippingAddress) { a.PostalCode = "1011" }, CashOnDelivery, postalFormat},
		{"missing payment", func(a *ShippingAddress) {}, "", required("paymentMethod")},
		{"unknown payment", func(a *ShippingAddress) {}, "BITCOIN",
			FieldError{"paymentMethod", "INVALID_FORMAT", "paymentMethod must be CASH_ON_DELIVERY or PAY_AT_STORE"}},
	}
	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			address := testAddress
			tt.change(&address)

			o, err := New(testCaller, []ItemInput{{"p1", 1}}, address, tt.payment, testNow)
			var apiErr *Error
			if o != nil || !errors.As(err, &apiErr) || apiErr.Code != CodeValidation {
				t.Fatalf("o = %v err = %v", o, err)
			}
			if len(apiErr.Fields) != 1 || apiErr.Fields[0] != tt.want {
				t.Errorf("fields = %+v, want %+v", apiErr.Fields, tt.want)
			}
		})
	}
}
