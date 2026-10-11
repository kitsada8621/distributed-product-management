package repository

import (
	"fmt"
	"time"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain"
)

const timeLayout = "2006-01-02T15:04:05Z"

func formatTime(t time.Time) string {
	return t.UTC().Format(timeLayout)
}

func parseTime(value string) (time.Time, error) {
	t, err := time.Parse(timeLayout, value)
	if err != nil {
		return time.Time{}, fmt.Errorf("parse time %q: %w", value, err)
	}
	return t, nil
}

func toModel(o *domain.Order) (orderModel, []orderItemModel) {
	model := orderModel{
		ID:            o.ID,
		UserID:        o.UserID,
		OwnerEmail:    o.OwnerEmail,
		Status:        string(o.Status),
		StockStatus:   string(o.StockStatus),
		TotalMinor:    o.TotalMinor,
		RecipientName: o.Address.RecipientName,
		Phone:         o.Address.Phone,
		AddressLine:   o.Address.AddressLine,
		Province:      o.Address.Province,
		PostalCode:    o.Address.PostalCode,
		PaymentMethod: string(o.Payment),
		CreatedAt:     formatTime(o.CreatedAt),
		UpdatedAt:     formatTime(o.UpdatedAt),
	}
	if o.Rejection != nil {
		rejectionType := string(o.Rejection.Type)
		model.RejectionType = &rejectionType
		model.RejectionMessage = o.Rejection.Message
	}

	items := make([]orderItemModel, 0, len(o.Items))
	for _, item := range o.Items {
		items = append(items, toItemModel(o.ID, item))
	}
	return model, items
}

func toItemModel(orderID string, item domain.Item) orderItemModel {
	model := orderItemModel{
		ID:             item.ID,
		OrderID:        orderID,
		ProductID:      item.ProductID,
		Quantity:       item.Quantity,
		ProductName:    item.Name,
		UnitPriceMinor: item.UnitPriceMinor,
	}
	if item.Failure != nil {
		reason := string(item.Failure.Reason)
		requested, available := item.Failure.Requested, item.Failure.Available
		model.FailureReason = &reason
		model.FailureRequested = &requested
		model.FailureAvailable = &available
	}
	return model
}

func toDomain(m orderModel, itemModels []orderItemModel) (*domain.Order, error) {
	createdAt, err := parseTime(m.CreatedAt)
	if err != nil {
		return nil, err
	}
	updatedAt, err := parseTime(m.UpdatedAt)
	if err != nil {
		return nil, err
	}

	o := &domain.Order{
		ID:          m.ID,
		UserID:      m.UserID,
		OwnerEmail:  m.OwnerEmail,
		Status:      domain.Status(m.Status),
		StockStatus: domain.StockStatus(m.StockStatus),
		TotalMinor:  m.TotalMinor,
		Address: domain.ShippingAddress{
			RecipientName: m.RecipientName,
			Phone:         m.Phone,
			AddressLine:   m.AddressLine,
			Province:      m.Province,
			PostalCode:    m.PostalCode,
		},
		Payment:   domain.PaymentMethod(m.PaymentMethod),
		CreatedAt: createdAt,
		UpdatedAt: updatedAt,
	}
	if m.RejectionType != nil {
		o.Rejection = &domain.Rejection{Type: domain.RejectionType(*m.RejectionType), Message: m.RejectionMessage}
	}
	for _, itemModel := range itemModels {
		o.Items = append(o.Items, toDomainItem(itemModel))
	}
	return o, nil
}

func toDomainItem(m orderItemModel) domain.Item {
	item := domain.Item{
		ID:             m.ID,
		ProductID:      m.ProductID,
		Quantity:       m.Quantity,
		Name:           m.ProductName,
		UnitPriceMinor: m.UnitPriceMinor,
	}
	if m.FailureReason != nil {
		item.Failure = &domain.ItemFailure{Reason: domain.FailureReason(*m.FailureReason)}
		if m.FailureRequested != nil {
			item.Failure.Requested = *m.FailureRequested
		}
		if m.FailureAvailable != nil {
			item.Failure.Available = *m.FailureAvailable
		}
	}
	return item
}
