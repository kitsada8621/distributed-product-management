package domain

import (
	"fmt"
	"math"
	"regexp"
	"strings"
	"time"
	"unicode/utf8"

	"github.com/google/uuid"
)

type Status string

const (
	Pending   Status = "Pending"
	Confirmed Status = "Confirmed"
	Rejected  Status = "Rejected"
	Cancelled Status = "Cancelled"
)

type StockStatus string

const (
	Reserving StockStatus = "Reserving"
	Reserved  StockStatus = "Reserved"
)

type RejectionType string

const (
	StockUnavailable RejectionType = "STOCK_UNAVAILABLE"
	TimeoutRejection RejectionType = "TIMEOUT"
	AdminRejected    RejectionType = "ADMIN_REJECTED"
)

type FailureReason string

const (
	InsufficientStock FailureReason = "INSUFFICIENT_STOCK"
	ProductNotFound   FailureReason = "PRODUCT_NOT_FOUND"
)

type PaymentMethod string

const (
	CashOnDelivery PaymentMethod = "CASH_ON_DELIVERY"
	PayAtStore     PaymentMethod = "PAY_AT_STORE"
)

type ShippingAddress struct {
	RecipientName string
	Phone         string
	AddressLine   string
	Province      string
	PostalCode    string
}

type Role string

const (
	RoleUser  Role = "user"
	RoleAdmin Role = "admin"
)

func (r Role) IsAdmin() bool { return r == RoleAdmin }

type Caller struct {
	UserID string
	Email  string
	Role   Role
}

type ItemFailure struct {
	Reason    FailureReason
	Requested int
	Available int
}

type Item struct {
	ID             string
	ProductID      string
	Quantity       int
	Name           *string
	UnitPriceMinor *int64
	Failure        *ItemFailure
}

type Rejection struct {
	Type    RejectionType
	Message *string
}

type Order struct {
	ID          string
	UserID      string
	OwnerEmail  string
	Status      Status
	StockStatus StockStatus
	TotalMinor  *int64
	Rejection   *Rejection
	Items       []Item
	Address     ShippingAddress
	Payment     PaymentMethod
	CreatedAt   time.Time
	UpdatedAt   time.Time
}

type State struct {
	Status      Status
	StockStatus StockStatus
}

func (o *Order) State() State {
	return State{Status: o.Status, StockStatus: o.StockStatus}
}

type ItemInput struct {
	ProductID string
	Quantity  float64
}

func New(caller Caller, inputs []ItemInput, address ShippingAddress, payment PaymentMethod, now time.Time) (*Order, error) {
	address = trimAddress(address)

	problems := validateInputs(inputs)
	problems = append(problems, validateAddress(address)...)
	problems = append(problems, validatePayment(payment)...)
	if len(problems) > 0 {
		return nil, NewValidationError(problems...)
	}

	now = now.UTC()
	items := make([]Item, 0, len(inputs))
	for _, input := range inputs {
		items = append(items, Item{
			ID:        uuid.NewString(),
			ProductID: input.ProductID,
			Quantity:  int(input.Quantity),
		})
	}
	return &Order{
		ID:          uuid.NewString(),
		UserID:      caller.UserID,
		OwnerEmail:  caller.Email,
		Status:      Pending,
		StockStatus: Reserving,
		Items:       items,
		Address:     address,
		Payment:     payment,
		CreatedAt:   now,
		UpdatedAt:   now,
	}, nil
}

func validateInputs(inputs []ItemInput) []FieldError {
	if len(inputs) == 0 {
		return []FieldError{{Field: "items", Code: "REQUIRED", Message: "At least one item is required"}}
	}

	var problems []FieldError
	seen := make(map[string]bool)
	for i, input := range inputs {
		productField := fmt.Sprintf("items[%d].productId", i)
		quantityField := fmt.Sprintf("items[%d].quantity", i)

		switch {
		case input.ProductID == "":
			problems = append(problems, FieldError{productField, "REQUIRED", "productId is required"})
		case seen[input.ProductID]:
			problems = append(problems, FieldError{productField, "DUPLICATE", "productId is repeated"})
		}
		seen[input.ProductID] = true

		switch {
		case input.Quantity != math.Trunc(input.Quantity):
			problems = append(problems, FieldError{quantityField, "INVALID_FORMAT", "quantity must be an integer"})
		case input.Quantity <= 0:
			problems = append(problems, FieldError{quantityField, "OUT_OF_RANGE", "quantity must be greater than 0"})
		}
	}
	return problems
}

var (
	phonePattern      = regexp.MustCompile(`^0\d{8,9}$`)
	postalCodePattern = regexp.MustCompile(`^\d{5}$`)
)

func trimAddress(a ShippingAddress) ShippingAddress {
	return ShippingAddress{
		RecipientName: strings.TrimSpace(a.RecipientName),
		Phone:         strings.TrimSpace(a.Phone),
		AddressLine:   strings.TrimSpace(a.AddressLine),
		Province:      strings.TrimSpace(a.Province),
		PostalCode:    strings.TrimSpace(a.PostalCode),
	}
}

func validateAddress(a ShippingAddress) []FieldError {
	var problems []FieldError
	problems = appendTextProblem(problems, "shippingAddress.recipientName", a.RecipientName, 100)
	problems = appendPatternProblem(problems, "shippingAddress.phone", a.Phone, phonePattern, "phone must be 9-10 digits starting with 0")
	problems = appendTextProblem(problems, "shippingAddress.addressLine", a.AddressLine, 255)
	problems = appendTextProblem(problems, "shippingAddress.province", a.Province, 100)
	problems = appendPatternProblem(problems, "shippingAddress.postalCode", a.PostalCode, postalCodePattern, "postalCode must be 5 digits")
	return problems
}

func appendTextProblem(problems []FieldError, field, value string, maxLength int) []FieldError {
	if value == "" {
		return append(problems, FieldError{field, "REQUIRED", field + " is required"})
	}
	if utf8.RuneCountInString(value) > maxLength {
		return append(problems, FieldError{field, "TOO_LONG", fmt.Sprintf("%s must be at most %d characters", field, maxLength)})
	}
	return problems
}

func appendPatternProblem(problems []FieldError, field, value string, pattern *regexp.Regexp, message string) []FieldError {
	if value == "" {
		return append(problems, FieldError{field, "REQUIRED", field + " is required"})
	}
	if !pattern.MatchString(value) {
		return append(problems, FieldError{field, "INVALID_FORMAT", message})
	}
	return problems
}

func validatePayment(payment PaymentMethod) []FieldError {
	switch payment {
	case CashOnDelivery, PayAtStore:
		return nil
	case "":
		return []FieldError{{"paymentMethod", "REQUIRED", "paymentMethod is required"}}
	}
	return []FieldError{{"paymentMethod", "INVALID_FORMAT", "paymentMethod must be CASH_ON_DELIVERY or PAY_AT_STORE"}}
}
