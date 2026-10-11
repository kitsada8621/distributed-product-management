package event

import (
	"encoding/json"
	"errors"
	"fmt"
	"time"

	"github.com/google/uuid"
)

const (
	TopicOrderCreated   = "order.created"
	TopicOrderConfirmed = "order.confirmed"
	TopicOrderCancelled = "order.cancelled"
	TopicStockReserved  = "stock.reserved"
	TopicStockRejected  = "stock.rejected"
	DLQPrefix           = "dlq."

	CorrelationHeader = "correlationId"

	ReasonUserCancelled = "USER_CANCELLED"
	ReasonAdminRejected = "ADMIN_REJECTED"
	ReasonTimeout       = "TIMEOUT"

	timeLayout     = "2006-01-02T15:04:05Z"
	currentVersion = 1
)

type Message struct {
	ID            string
	Topic         string
	Key           string
	Payload       []byte
	CorrelationID string
}

type Item struct {
	ProductID string `json:"productId"`
	Quantity  int    `json:"quantity"`
}

type header struct {
	EventID    string `json:"eventId"`
	OccurredAt string `json:"occurredAt"`
	Version    int    `json:"version"`
}

func newHeader(now time.Time) header {
	return header{EventID: uuid.NewString(), OccurredAt: now.UTC().Format(timeLayout), Version: currentVersion}
}

type orderCreated struct {
	header
	OrderID string `json:"orderId"`
	UserID  string `json:"userId"`
	Items   []Item `json:"items"`
}

type orderConfirmed struct {
	header
	OrderID string `json:"orderId"`
}

type orderCancelled struct {
	header
	OrderID string `json:"orderId"`
	Reason  string `json:"reason"`
}

type deadLetter struct {
	header
	OriginalTopic   string `json:"originalTopic"`
	OriginalMessage string `json:"originalMessage"`
	Error           string `json:"error"`
	Attempts        int    `json:"attempts"`
}

func build(topic, key, eventID, correlationID string, payload any) Message {
	body, err := json.Marshal(payload)
	if err != nil {

		panic(fmt.Sprintf("marshal %s: %v", topic, err))
	}
	return Message{ID: eventID, Topic: topic, Key: key, Payload: body, CorrelationID: correlationID}
}

func NewOrderCreated(orderID, userID string, items []Item, correlationID string, now time.Time) Message {
	h := newHeader(now)
	return build(TopicOrderCreated, orderID, h.EventID, correlationID,
		orderCreated{header: h, OrderID: orderID, UserID: userID, Items: items})
}

func NewOrderConfirmed(orderID, correlationID string, now time.Time) Message {
	h := newHeader(now)
	return build(TopicOrderConfirmed, orderID, h.EventID, correlationID, orderConfirmed{header: h, OrderID: orderID})
}

func NewOrderCancelled(orderID, reason, correlationID string, now time.Time) Message {
	h := newHeader(now)
	return build(TopicOrderCancelled, orderID, h.EventID, correlationID,
		orderCancelled{header: h, OrderID: orderID, Reason: reason})
}

func NewDeadLetter(topic, key string, original []byte, errText string, attempts int, correlationID string, now time.Time) Message {
	h := newHeader(now)
	return build(DLQPrefix+topic, key, h.EventID, correlationID, deadLetter{
		header:          h,
		OriginalTopic:   topic,
		OriginalMessage: string(original),
		Error:           errText,
		Attempts:        attempts,
	})
}

type ReservedItem struct {
	ProductID string  `json:"productId"`
	Name      string  `json:"name"`
	UnitPrice float64 `json:"unitPrice"`
	Quantity  int     `json:"quantity"`
}

type StockReserved struct {
	EventID string         `json:"eventId"`
	OrderID string         `json:"orderId"`
	Items   []ReservedItem `json:"items"`
	Version int            `json:"version"`
}

type FailedItem struct {
	ProductID string `json:"productId"`
	Reason    string `json:"reason"`
	Requested int    `json:"requested"`
	Available int    `json:"available"`
}

type StockRejected struct {
	EventID     string       `json:"eventId"`
	OrderID     string       `json:"orderId"`
	FailedItems []FailedItem `json:"failedItems"`
	Version     int          `json:"version"`
}

func DecodeStockReserved(body []byte) (StockReserved, error) {
	var ev StockReserved
	if err := json.Unmarshal(body, &ev); err != nil {
		return StockReserved{}, fmt.Errorf("decode stock.reserved: %w", err)
	}
	if err := checkCommon(ev.EventID, ev.OrderID, ev.Version); err != nil {
		return StockReserved{}, fmt.Errorf("stock.reserved: %w", err)
	}
	return ev, nil
}

func DecodeStockRejected(body []byte) (StockRejected, error) {
	var ev StockRejected
	if err := json.Unmarshal(body, &ev); err != nil {
		return StockRejected{}, fmt.Errorf("decode stock.rejected: %w", err)
	}
	if err := checkCommon(ev.EventID, ev.OrderID, ev.Version); err != nil {
		return StockRejected{}, fmt.Errorf("stock.rejected: %w", err)
	}
	return ev, nil
}

func checkCommon(eventID, orderID string, version int) error {
	if eventID == "" || orderID == "" {
		return errors.New("eventId and orderId are required")
	}
	if version != currentVersion {
		return fmt.Errorf("unsupported version %d", version)
	}
	return nil
}
