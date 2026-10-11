package repository

import (
	"context"
	"time"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain"
)

func (s *Store) Save(ctx context.Context, o *domain.Order, before domain.State) error {
	model, items := toModel(o)
	db := s.db.WithContext(ctx)

	result := db.Model(&orderModel{}).
		Where("id = ? AND status = ? AND stock_status = ?", o.ID, before.Status, before.StockStatus).
		Updates(map[string]any{
			"status":            model.Status,
			"stock_status":      model.StockStatus,
			"total_minor":       model.TotalMinor,
			"rejection_type":    model.RejectionType,
			"rejection_message": model.RejectionMessage,
			"updated_at":        model.UpdatedAt,
		})
	if result.Error != nil {
		return result.Error
	}
	if result.RowsAffected == 0 {
		return domain.ErrStateChanged
	}

	for _, item := range items {
		err := db.Model(&orderItemModel{}).Where("id = ?", item.ID).Updates(map[string]any{
			"product_name":      item.ProductName,
			"unit_price_minor":  item.UnitPriceMinor,
			"failure_reason":    item.FailureReason,
			"failure_requested": item.FailureRequested,
			"failure_available": item.FailureAvailable,
		}).Error
		if err != nil {
			return err
		}
	}
	return nil
}

func (s *Store) FindStale(ctx context.Context, createdBefore time.Time, limit int) ([]*domain.Order, error) {
	var models []orderModel
	err := s.db.WithContext(ctx).
		Where("status = 'Pending' AND stock_status = 'Reserving' AND created_at < ?", formatTime(createdBefore)).
		Order("created_at").
		Limit(limit).
		Find(&models).Error
	if err != nil {
		return nil, err
	}

	orders := make([]*domain.Order, 0, len(models))
	for _, model := range models {
		o, err := s.withItems(ctx, model)
		if err != nil {
			return nil, err
		}
		orders = append(orders, o)
	}
	return orders, nil
}
