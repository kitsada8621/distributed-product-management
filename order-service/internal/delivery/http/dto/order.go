package dto

import (
	"time"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain"
)

const timeLayout = "2006-01-02T15:04:05Z"

type CreateResponse struct {
	OrderID     string  `json:"orderId"`
	Status      string  `json:"status"`
	StockStatus *string `json:"stockStatus"`
}

type itemResponse struct {
	ProductID string   `json:"productId"`
	Name      *string  `json:"name"`
	UnitPrice *float64 `json:"unitPrice"`
	Quantity  int      `json:"quantity"`
}

type failedItemResponse struct {
	ProductID string `json:"productId"`
	Reason    string `json:"reason"`
	Requested int    `json:"requested"`
	Available int    `json:"available"`
}

type rejectionResponse struct {
	Type        string               `json:"type"`
	Message     *string              `json:"message"`
	FailedItems []failedItemResponse `json:"failedItems"`
}

type shippingAddressResponse struct {
	RecipientName string `json:"recipientName"`
	Phone         string `json:"phone"`
	AddressLine   string `json:"addressLine"`
	Province      string `json:"province"`
	PostalCode    string `json:"postalCode"`
}

type OrderResponse struct {
	ID              string                  `json:"id"`
	Status          string                  `json:"status"`
	StockStatus     *string                 `json:"stockStatus"`
	Items           []itemResponse          `json:"items"`
	Total           *float64                `json:"total"`
	ShippingAddress shippingAddressResponse `json:"shippingAddress"`
	PaymentMethod   string                  `json:"paymentMethod"`
	Rejection       *rejectionResponse      `json:"rejection"`
	OwnerEmail      *string                 `json:"ownerEmail,omitempty"`
	CreatedAt       string                  `json:"createdAt"`
	UpdatedAt       string                  `json:"updatedAt"`
}

type RowResponse struct {
	ID          string   `json:"id"`
	Status      string   `json:"status"`
	StockStatus *string  `json:"stockStatus"`
	Total       *float64 `json:"total"`
	OwnerEmail  *string  `json:"ownerEmail,omitempty"`
	CreatedAt   string   `json:"createdAt"`
}

func ToCreateResponse(o *domain.Order) CreateResponse {
	return CreateResponse{
		OrderID:     o.ID,
		Status:      string(o.Status),
		StockStatus: visibleStockStatus(o.Status, o.StockStatus),
	}
}

func ToOrderResponse(o *domain.Order, withOwner bool) OrderResponse {
	response := OrderResponse{
		ID:          o.ID,
		Status:      string(o.Status),
		StockStatus: visibleStockStatus(o.Status, o.StockStatus),
		Items:       make([]itemResponse, 0, len(o.Items)),
		Total:       money(o.TotalMinor),
		ShippingAddress: shippingAddressResponse{
			RecipientName: o.Address.RecipientName,
			Phone:         o.Address.Phone,
			AddressLine:   o.Address.AddressLine,
			Province:      o.Address.Province,
			PostalCode:    o.Address.PostalCode,
		},
		PaymentMethod: string(o.Payment),
		Rejection:     toRejectionResponse(o),
		CreatedAt:     formatTime(o.CreatedAt),
		UpdatedAt:     formatTime(o.UpdatedAt),
	}
	for _, item := range o.Items {
		response.Items = append(response.Items, itemResponse{
			ProductID: item.ProductID,
			Name:      item.Name,
			UnitPrice: money(item.UnitPriceMinor),
			Quantity:  item.Quantity,
		})
	}
	if withOwner {
		response.OwnerEmail = &o.OwnerEmail
	}
	return response
}

func ToRowResponse(r domain.OrderRow, withOwner bool) RowResponse {
	response := RowResponse{
		ID:          r.ID,
		Status:      string(r.Status),
		StockStatus: visibleStockStatus(r.Status, r.StockStatus),
		Total:       money(r.TotalMinor),
		CreatedAt:   formatTime(r.CreatedAt),
	}
	if withOwner {
		response.OwnerEmail = &r.OwnerEmail
	}
	return response
}

func toRejectionResponse(o *domain.Order) *rejectionResponse {
	if o.Status != domain.Rejected || o.Rejection == nil {
		return nil
	}
	failed := []failedItemResponse{}
	for _, item := range o.Items {
		if item.Failure == nil {
			continue
		}
		failed = append(failed, failedItemResponse{
			ProductID: item.ProductID,
			Reason:    string(item.Failure.Reason),
			Requested: item.Failure.Requested,
			Available: item.Failure.Available,
		})
	}
	return &rejectionResponse{Type: string(o.Rejection.Type), Message: o.Rejection.Message, FailedItems: failed}
}

func visibleStockStatus(status domain.Status, stock domain.StockStatus) *string {
	if status != domain.Pending {
		return nil
	}
	value := string(stock)
	return &value
}

func money(minor *int64) *float64 {
	if minor == nil {
		return nil
	}
	value := float64(*minor) / 100
	return &value
}

func formatTime(t time.Time) string {
	return t.UTC().Format(timeLayout)
}
