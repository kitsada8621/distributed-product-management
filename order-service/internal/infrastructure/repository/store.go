package repository

import (
	"context"
	"errors"

	"github.com/mattn/go-sqlite3"
	"gorm.io/gorm"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain"
)

type Store struct {
	db *gorm.DB
}

func NewStore(db *gorm.DB) *Store {
	return &Store{db: db}
}

func (s *Store) WithinTx(ctx context.Context, fn func(ctx context.Context, tx domain.Repository) error) error {
	return s.db.WithContext(ctx).Transaction(func(tx *gorm.DB) error {
		return fn(ctx, &Store{db: tx})
	})
}

func (s *Store) Create(ctx context.Context, o *domain.Order, key, requestHash string) error {
	model, items := toModel(o)
	db := s.db.WithContext(ctx)

	if err := db.Create(&model).Error; err != nil {
		return err
	}
	if len(items) > 0 {
		if err := db.Create(&items).Error; err != nil {
			return err
		}
	}

	record := idempotencyModel{
		UserID:         o.UserID,
		IdempotencyKey: key,
		OrderID:        o.ID,
		RequestHash:    requestHash,
		CreatedAt:      formatTime(o.CreatedAt),
	}
	if err := db.Create(&record).Error; err != nil {
		if isUniqueViolation(err) {
			return domain.ErrDuplicateKey
		}
		return err
	}
	return nil
}

func (s *Store) FindIdempotency(ctx context.Context, userID, key string) (*domain.IdempotencyRecord, error) {
	var record idempotencyModel
	err := s.db.WithContext(ctx).
		Where("user_id = ? AND idempotency_key = ?", userID, key).
		First(&record).Error
	if errors.Is(err, gorm.ErrRecordNotFound) {
		return nil, nil
	}
	if err != nil {
		return nil, err
	}

	o, err := s.GetByID(ctx, record.OrderID)
	if err != nil {
		return nil, err
	}
	return &domain.IdempotencyRecord{RequestHash: record.RequestHash, Order: o}, nil
}

func (s *Store) GetByID(ctx context.Context, id string) (*domain.Order, error) {
	return s.loadOne(ctx, s.db.WithContext(ctx).Where("id = ?", id))
}

func (s *Store) GetOwned(ctx context.Context, id, userID string) (*domain.Order, error) {
	return s.loadOne(ctx, s.db.WithContext(ctx).Where("id = ? AND user_id = ?", id, userID))
}

func (s *Store) loadOne(ctx context.Context, query *gorm.DB) (*domain.Order, error) {
	var model orderModel
	if err := query.First(&model).Error; err != nil {
		if errors.Is(err, gorm.ErrRecordNotFound) {
			return nil, domain.ErrNotFound
		}
		return nil, err
	}
	return s.withItems(ctx, model)
}

func (s *Store) withItems(ctx context.Context, model orderModel) (*domain.Order, error) {
	var items []orderItemModel

	if err := s.db.WithContext(ctx).Where("order_id = ?", model.ID).Order("rowid").Find(&items).Error; err != nil {
		return nil, err
	}
	return toDomain(model, items)
}

func isUniqueViolation(err error) bool {
	var sqliteErr sqlite3.Error
	if !errors.As(err, &sqliteErr) {
		return false
	}
	return sqliteErr.ExtendedCode == sqlite3.ErrConstraintPrimaryKey ||
		sqliteErr.ExtendedCode == sqlite3.ErrConstraintUnique
}
