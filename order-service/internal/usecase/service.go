package usecase

import (
	"time"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain"
)

type Service struct {
	store domain.Store
	now   func() time.Time
}

func NewService(store domain.Store, now func() time.Time) *Service {
	return &Service{store: store, now: now}
}
