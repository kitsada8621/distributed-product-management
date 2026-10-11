package repository

type orderModel struct {
	ID               string  `gorm:"column:id;primaryKey"`
	UserID           string  `gorm:"column:user_id"`
	OwnerEmail       string  `gorm:"column:owner_email"`
	Status           string  `gorm:"column:status"`
	StockStatus      string  `gorm:"column:stock_status"`
	TotalMinor       *int64  `gorm:"column:total_minor"`
	RejectionType    *string `gorm:"column:rejection_type"`
	RejectionMessage *string `gorm:"column:rejection_message"`
	RecipientName    string  `gorm:"column:recipient_name"`
	Phone            string  `gorm:"column:phone"`
	AddressLine      string  `gorm:"column:address_line"`
	Province         string  `gorm:"column:province"`
	PostalCode       string  `gorm:"column:postal_code"`
	PaymentMethod    string  `gorm:"column:payment_method"`
	CreatedAt        string  `gorm:"column:created_at"`
	UpdatedAt        string  `gorm:"column:updated_at"`
}

func (orderModel) TableName() string { return "orders" }

type orderItemModel struct {
	ID               string  `gorm:"column:id;primaryKey"`
	OrderID          string  `gorm:"column:order_id"`
	ProductID        string  `gorm:"column:product_id"`
	Quantity         int     `gorm:"column:quantity"`
	ProductName      *string `gorm:"column:product_name"`
	UnitPriceMinor   *int64  `gorm:"column:unit_price_minor"`
	FailureReason    *string `gorm:"column:failure_reason"`
	FailureRequested *int    `gorm:"column:failure_requested"`
	FailureAvailable *int    `gorm:"column:failure_available"`
}

func (orderItemModel) TableName() string { return "order_items" }

type idempotencyModel struct {
	UserID         string `gorm:"column:user_id;primaryKey"`
	IdempotencyKey string `gorm:"column:idempotency_key;primaryKey"`
	OrderID        string `gorm:"column:order_id"`
	RequestHash    string `gorm:"column:request_hash"`
	CreatedAt      string `gorm:"column:created_at"`
}

func (idempotencyModel) TableName() string { return "idempotency_keys" }

type outboxModel struct {
	ID            string  `gorm:"column:id;primaryKey"`
	Topic         string  `gorm:"column:topic"`
	MessageKey    string  `gorm:"column:message_key"`
	Payload       string  `gorm:"column:payload"`
	CorrelationID string  `gorm:"column:correlation_id"`
	Status        string  `gorm:"column:status"`
	Attempts      int     `gorm:"column:attempts"`
	NextAttemptAt string  `gorm:"column:next_attempt_at"`
	CreatedAt     string  `gorm:"column:created_at"`
	SentAt        *string `gorm:"column:sent_at"`
}

func (outboxModel) TableName() string { return "outbox" }

type inboxModel struct {
	EventID     string `gorm:"column:event_id;primaryKey"`
	Topic       string `gorm:"column:topic"`
	ProcessedAt string `gorm:"column:processed_at"`
}

func (inboxModel) TableName() string { return "inbox" }
