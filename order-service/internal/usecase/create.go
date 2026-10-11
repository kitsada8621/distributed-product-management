package usecase

import (
	"context"
	"crypto/sha256"
	"encoding/hex"
	"errors"
	"fmt"
	"sort"
	"strings"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain/event"
	"github.com/kitsada8621/distributed-product-management/order-service/pkg/correlation"
)

type CreateResult struct {
	Order   *domain.Order
	Created bool
}

func (s *Service) CreateOrder(ctx context.Context, caller domain.Caller, key string, inputs []domain.ItemInput, address domain.ShippingAddress, payment domain.PaymentMethod) (CreateResult, error) {
	key = strings.TrimSpace(key)
	if key == "" {
		return CreateResult{}, &domain.Error{Code: domain.CodeIdempotencyKeyRequired, Message: "Idempotency-Key header is required"}
	}
	o, err := domain.New(caller, inputs, address, payment, s.now())
	if err != nil {
		return CreateResult{}, err
	}
	hash := requestHash(inputs, o.Address, o.Payment)

	var result CreateResult
	err = s.store.WithinTx(ctx, func(ctx context.Context, tx domain.Repository) error {
		existing, err := tx.FindIdempotency(ctx, caller.UserID, key)
		if err != nil {
			return err
		}
		if existing != nil {
			result, err = sameRequest(existing, hash)
			return err
		}

		if err := tx.Create(ctx, o, key, hash); err != nil {
			return err
		}
		msg := event.NewOrderCreated(o.ID, o.UserID, eventItems(o), correlation.From(ctx), s.now())
		if err := tx.Enqueue(ctx, msg); err != nil {
			return err
		}
		result = CreateResult{Order: o, Created: true}
		return nil
	})
	if errors.Is(err, domain.ErrDuplicateKey) {

		return s.findExisting(ctx, caller.UserID, key, hash)
	}
	return result, err
}

func (s *Service) findExisting(ctx context.Context, userID, key, hash string) (CreateResult, error) {
	existing, err := s.store.FindIdempotency(ctx, userID, key)
	if err != nil {
		return CreateResult{}, err
	}
	if existing == nil {
		return CreateResult{}, domain.ErrDuplicateKey
	}
	return sameRequest(existing, hash)
}

func sameRequest(existing *domain.IdempotencyRecord, hash string) (CreateResult, error) {
	if existing.RequestHash != hash {
		return CreateResult{}, &domain.Error{Code: domain.CodeIdempotencyKeyReused, Message: "Idempotency-Key was used with a different request"}
	}
	return CreateResult{Order: existing.Order, Created: false}, nil
}

func eventItems(o *domain.Order) []event.Item {
	items := make([]event.Item, 0, len(o.Items))
	for _, item := range o.Items {
		items = append(items, event.Item{ProductID: item.ProductID, Quantity: item.Quantity})
	}
	return items
}

func requestHash(inputs []domain.ItemInput, address domain.ShippingAddress, payment domain.PaymentMethod) string {
	lines := make([]string, 0, len(inputs))
	for _, input := range inputs {
		lines = append(lines, fmt.Sprintf("%s:%v", input.ProductID, input.Quantity))
	}
	sort.Strings(lines)
	lines = append(lines, address.RecipientName, address.Phone, address.AddressLine, address.Province, address.PostalCode, string(payment))
	sum := sha256.Sum256([]byte(strings.Join(lines, "\n")))
	return hex.EncodeToString(sum[:])
}
