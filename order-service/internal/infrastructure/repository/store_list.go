package repository

import (
	"context"

	"gorm.io/gorm"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain"
)

func (s *Store) ListByUser(ctx context.Context, userID string, f domain.ListFilter) (domain.OrderPage, error) {
	return s.list(ctx, s.db.WithContext(ctx).Where("user_id = ?", userID), f)
}

func (s *Store) ListAll(ctx context.Context, f domain.ListFilter) (domain.OrderPage, error) {
	return s.list(ctx, s.db.WithContext(ctx), f)
}

func (s *Store) list(_ context.Context, base *gorm.DB, f domain.ListFilter) (domain.OrderPage, error) {
	if f.Status != nil {
		base = base.Where("status = ?", string(*f.Status))
	}

	var total int64
	if err := base.Session(&gorm.Session{}).Model(&orderModel{}).Count(&total).Error; err != nil {
		return domain.OrderPage{}, err
	}

	var models []orderModel
	err := base.Session(&gorm.Session{}).
		Select("id", "owner_email", "status", "stock_status", "total_minor", "created_at").
		Order("created_at DESC, id DESC").
		Limit(f.PageSize).
		Offset((f.Page - 1) * f.PageSize).
		Find(&models).Error
	if err != nil {
		return domain.OrderPage{}, err
	}

	rows := make([]domain.OrderRow, 0, len(models))
	for _, model := range models {
		createdAt, err := parseTime(model.CreatedAt)
		if err != nil {
			return domain.OrderPage{}, err
		}
		rows = append(rows, domain.OrderRow{
			ID:          model.ID,
			OwnerEmail:  model.OwnerEmail,
			Status:      domain.Status(model.Status),
			StockStatus: domain.StockStatus(model.StockStatus),
			TotalMinor:  model.TotalMinor,
			CreatedAt:   createdAt,
		})
	}
	return domain.OrderPage{Rows: rows, Total: int(total)}, nil
}
