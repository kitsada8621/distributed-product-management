-- +goose Up
CREATE TABLE orders (
    id                TEXT PRIMARY KEY,
    user_id           TEXT NOT NULL,
    owner_email       TEXT NOT NULL,
    status            TEXT NOT NULL DEFAULT 'Pending'
                      CHECK (status IN ('Pending', 'Confirmed', 'Rejected', 'Cancelled')),
    stock_status      TEXT NOT NULL DEFAULT 'Reserving'
                      CHECK (stock_status IN ('Reserving', 'Reserved')),
    total_minor       INTEGER CHECK (total_minor >= 0),
    rejection_type    TEXT
                      CHECK (rejection_type IN ('STOCK_UNAVAILABLE', 'TIMEOUT', 'ADMIN_REJECTED')),
    rejection_message TEXT,
    recipient_name    TEXT NOT NULL,
    phone             TEXT NOT NULL,
    address_line      TEXT NOT NULL,
    province          TEXT NOT NULL,
    postal_code       TEXT NOT NULL,
    payment_method    TEXT NOT NULL
                      CHECK (payment_method IN ('CASH_ON_DELIVERY', 'PAY_AT_STORE')),
    created_at        TEXT NOT NULL,
    updated_at        TEXT NOT NULL
);
CREATE INDEX ix_orders_user ON orders(user_id, created_at DESC);
CREATE INDEX ix_orders_status ON orders(status, created_at DESC);
CREATE INDEX ix_orders_reserving ON orders(created_at)
    WHERE status = 'Pending' AND stock_status = 'Reserving';

CREATE TABLE order_items (
    id                  TEXT PRIMARY KEY,
    order_id            TEXT NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    product_id          TEXT NOT NULL,
    quantity            INTEGER NOT NULL CHECK (quantity > 0),
    product_name        TEXT,
    unit_price_minor    INTEGER CHECK (unit_price_minor > 0),
    failure_reason      TEXT
                        CHECK (failure_reason IN ('INSUFFICIENT_STOCK', 'PRODUCT_NOT_FOUND')),
    failure_requested   INTEGER,
    failure_available   INTEGER,
    UNIQUE (order_id, product_id)
);

CREATE TABLE idempotency_keys (
    user_id          TEXT NOT NULL,
    idempotency_key  TEXT NOT NULL,
    order_id         TEXT NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    request_hash     TEXT NOT NULL,
    created_at       TEXT NOT NULL,
    PRIMARY KEY (user_id, idempotency_key)
);

CREATE TABLE outbox (
    id               TEXT PRIMARY KEY,
    topic            TEXT NOT NULL,
    message_key      TEXT NOT NULL,
    payload          TEXT NOT NULL,
    correlation_id   TEXT NOT NULL,
    status           TEXT NOT NULL DEFAULT 'Pending' CHECK (status IN ('Pending', 'Sent')),
    attempts         INTEGER NOT NULL DEFAULT 0,
    next_attempt_at  TEXT NOT NULL,
    created_at       TEXT NOT NULL,
    sent_at          TEXT
);
CREATE INDEX ix_outbox_pending ON outbox(next_attempt_at) WHERE status = 'Pending';

CREATE TABLE inbox (
    event_id      TEXT PRIMARY KEY,
    topic         TEXT NOT NULL,
    processed_at  TEXT NOT NULL
);

-- +goose Down
DROP TABLE inbox;
DROP TABLE outbox;
DROP TABLE idempotency_keys;
DROP TABLE order_items;
DROP TABLE orders;
