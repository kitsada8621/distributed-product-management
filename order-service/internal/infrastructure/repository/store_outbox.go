package repository

import (
	"context"
	"time"

	"gorm.io/gorm/clause"

	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain"
	"github.com/kitsada8621/distributed-product-management/order-service/internal/domain/event"
)

var (
	_ domain.Store       = (*Store)(nil)
	_ domain.OutboxStore = (*Store)(nil)
)

func (s *Store) Enqueue(ctx context.Context, msg event.Message) error {
	now := formatTime(time.Now())
	row := outboxModel{
		ID:            msg.ID,
		Topic:         msg.Topic,
		MessageKey:    msg.Key,
		Payload:       string(msg.Payload),
		CorrelationID: msg.CorrelationID,
		Status:        "Pending",
		NextAttemptAt: now,
		CreatedAt:     now,
	}
	return s.db.WithContext(ctx).Create(&row).Error
}

func (s *Store) MarkProcessed(ctx context.Context, eventID, topic string) (bool, error) {
	row := inboxModel{EventID: eventID, Topic: topic, ProcessedAt: formatTime(time.Now())}
	result := s.db.WithContext(ctx).Clauses(clause.OnConflict{DoNothing: true}).Create(&row)
	if result.Error != nil {
		return false, result.Error
	}
	return result.RowsAffected == 1, nil
}

func (s *Store) FetchDue(ctx context.Context, now time.Time, limit int) ([]domain.OutboxEntry, error) {
	var rows []outboxModel
	err := s.db.WithContext(ctx).
		Where("status = 'Pending' AND next_attempt_at <= ?", formatTime(now)).
		Order("created_at, rowid").
		Limit(limit).
		Find(&rows).Error
	if err != nil {
		return nil, err
	}

	entries := make([]domain.OutboxEntry, 0, len(rows))
	for _, row := range rows {
		entries = append(entries, domain.OutboxEntry{
			Message: event.Message{
				ID:            row.ID,
				Topic:         row.Topic,
				Key:           row.MessageKey,
				Payload:       []byte(row.Payload),
				CorrelationID: row.CorrelationID,
			},
			Attempts: row.Attempts,
		})
	}
	return entries, nil
}

func (s *Store) MarkSent(ctx context.Context, id string, at time.Time) error {
	return s.db.WithContext(ctx).Model(&outboxModel{}).Where("id = ?", id).
		Updates(map[string]any{"status": "Sent", "sent_at": formatTime(at)}).Error
}

func (s *Store) MarkFailed(ctx context.Context, id string, attempts int, next time.Time) error {
	return s.db.WithContext(ctx).Model(&outboxModel{}).Where("id = ?", id).
		Updates(map[string]any{"attempts": attempts, "next_attempt_at": formatTime(next)}).Error
}
