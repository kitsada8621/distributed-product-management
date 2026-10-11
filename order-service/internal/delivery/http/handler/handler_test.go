package handler

import (
	"context"
	"encoding/json"
	"io"
	"net/http"
	"net/http/httptest"
	"strings"
	"testing"
	"time"

	"github.com/gofiber/fiber/v3"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/delivery/http/middleware"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/usecase"
)

var testAddress = domain.ShippingAddress{
	RecipientName: "Somchai Jaidee",
	Phone:         "0812345678",
	AddressLine:   "99/9 Soi Sukhumvit 21",
	Province:      "Bangkok",
	PostalCode:    "10110",
}

const validBody = `{
	"items": [{"productId": "p1", "quantity": 2}],
	"shippingAddress": {
		"recipientName": "Somchai Jaidee",
		"phone": "0812345678",
		"addressLine": "99/9 Soi Sukhumvit 21",
		"province": "Bangkok",
		"postalCode": "10110"
	},
	"paymentMethod": "PAY_AT_STORE"
}`

type fakeService struct {
	OrderService
	createErr error
	order     *domain.Order

	gotAddress domain.ShippingAddress
	gotPayment domain.PaymentMethod
}

func (f *fakeService) CreateOrder(_ context.Context, _ domain.Caller, _ string, _ []domain.ItemInput, address domain.ShippingAddress, payment domain.PaymentMethod) (usecase.CreateResult, error) {
	f.gotAddress = address
	f.gotPayment = payment
	if f.createErr != nil {
		return usecase.CreateResult{}, f.createErr
	}
	return usecase.CreateResult{Order: f.order, Created: true}, nil
}

func (f *fakeService) GetMine(_ context.Context, _, _ string) (*domain.Order, error) {
	return f.order, nil
}

type fakeVerifier struct{}

func (fakeVerifier) Verify(string) (domain.Caller, error) {
	return domain.Caller{UserID: "u1", Email: "u1@example.com", Role: domain.RoleUser}, nil
}

func newTestApp(service OrderService) *fiber.App {
	app := fiber.New(fiber.Config{ErrorHandler: middleware.ErrorHandler})
	h := NewHandler(service)
	authenticate := middleware.Authenticate(fakeVerifier{})
	app.Post("/api/orders", authenticate, h.CreateOrder)
	app.Get("/api/orders/:id", authenticate, h.GetOrder)
	return app
}

func newTestOrder(t *testing.T) *domain.Order {
	t.Helper()
	caller := domain.Caller{UserID: "u1", Email: "u1@example.com", Role: domain.RoleUser}
	o, err := domain.New(caller, []domain.ItemInput{{ProductID: "p1", Quantity: 2}}, testAddress, domain.PayAtStore, time.Now())
	if err != nil {
		t.Fatal(err)
	}
	return o
}

func send(t *testing.T, app *fiber.App, req *http.Request) (int, string) {
	t.Helper()
	req.Header.Set("Authorization", "Bearer token")
	req.Header.Set("Content-Type", "application/json")
	resp, err := app.Test(req)
	if err != nil {
		t.Fatal(err)
	}
	defer resp.Body.Close()
	body, _ := io.ReadAll(resp.Body)
	return resp.StatusCode, string(body)
}

func TestCreateOrderPassesAddressAndPayment(t *testing.T) {
	service := &fakeService{order: newTestOrder(t)}
	req := httptest.NewRequest(http.MethodPost, "/api/orders", strings.NewReader(validBody))
	req.Header.Set("Idempotency-Key", "key-1")

	status, body := send(t, newTestApp(service), req)

	if status != http.StatusAccepted {
		t.Fatalf("status = %d body = %s", status, body)
	}
	if service.gotAddress != testAddress || service.gotPayment != domain.PayAtStore {
		t.Errorf("address = %+v payment = %s", service.gotAddress, service.gotPayment)
	}
}

func TestCreateOrderReturnsFieldErrorFor422(t *testing.T) {
	service := &fakeService{createErr: domain.NewValidationError(
		domain.FieldError{Field: "shippingAddress.phone", Code: "INVALID_FORMAT", Message: "phone must be 9-10 digits starting with 0"},
	)}
	req := httptest.NewRequest(http.MethodPost, "/api/orders", strings.NewReader(validBody))
	req.Header.Set("Idempotency-Key", "key-1")

	status, body := send(t, newTestApp(service), req)

	if status != http.StatusUnprocessableEntity {
		t.Fatalf("status = %d body = %s", status, body)
	}
	var response struct {
		Errors []struct{ Field, Code string }
	}
	if err := json.Unmarshal([]byte(body), &response); err != nil {
		t.Fatal(err)
	}
	if len(response.Errors) != 1 || response.Errors[0].Field != "shippingAddress.phone" || response.Errors[0].Code != "INVALID_FORMAT" {
		t.Errorf("errors = %+v", response.Errors)
	}
}

func TestGetOrderReturnsAddressAndPayment(t *testing.T) {
	service := &fakeService{order: newTestOrder(t)}
	req := httptest.NewRequest(http.MethodGet, "/api/orders/"+service.order.ID, nil)

	status, body := send(t, newTestApp(service), req)

	if status != http.StatusOK {
		t.Fatalf("status = %d body = %s", status, body)
	}
	var response struct {
		Data struct {
			ShippingAddress struct{ PostalCode, Phone string }
			PaymentMethod   string
		}
	}
	if err := json.Unmarshal([]byte(body), &response); err != nil {
		t.Fatal(err)
	}
	if response.Data.ShippingAddress.PostalCode != "10110" || response.Data.ShippingAddress.Phone != "0812345678" || response.Data.PaymentMethod != "PAY_AT_STORE" {
		t.Errorf("data = %+v", response.Data)
	}
}
