# ER Diagram

แต่ละบริการมีฐานข้อมูล SQLite ของตนเอง จึงไม่มี foreign key ข้ามบริการ การอ้างอิงข้ามบริการเก็บเป็นค่า id ธรรมดา (เส้นประในแผนภาพแรก) และรักษาความสอดคล้องด้วย Kafka event

## 1. ภาพรวมข้ามบริการ

```mermaid
erDiagram
    USERS ||..o{ ORDERS : "user_id (อ้างด้วยค่า)"
    PRODUCTS ||..o{ ORDER_ITEMS : "product_id (อ้างด้วยค่า)"
    ORDERS ||--o{ ORDER_ITEMS : "มีหลายรายการ"
    ORDERS ||..o| RESERVATIONS : "order_id (อ้างด้วยค่า)"

    USERS { text id PK }
    PRODUCTS { text id PK }
    ORDERS { text id PK }
    ORDER_ITEMS { text id PK }
    RESERVATIONS { text order_id PK }
```

| บริการ | ตารางหลัก |
|---|---|
| Auth (`auth.db`) | `users` |
| Catalog (`catalog.db`) | `products`, `reservations` |
| Order (`order.db`) | `orders`, `order_items` |

## 2. Auth Service (`auth.db`)

```mermaid
erDiagram
    users ||--o{ refresh_tokens : "has"
    users ||--o{ reset_tokens : "requests"
    users ||--o{ email_outbox : "receives"

    users {
        TEXT id PK
        TEXT email UK
        TEXT name
        TEXT password_hash
        TEXT role "user | admin"
        INTEGER failed_attempts
        TEXT locked_until "nullable"
        TEXT created_at
        TEXT updated_at
    }
    refresh_tokens {
        TEXT id PK
        TEXT user_id FK
        TEXT family_id
        TEXT token_hash UK
        TEXT expires_at
        TEXT used_at "nullable"
        TEXT revoked_at "nullable"
        TEXT created_at
    }
    reset_tokens {
        TEXT id PK
        TEXT user_id FK
        TEXT token_hash UK
        TEXT expires_at
        TEXT used_at "nullable"
        TEXT created_at
    }
    email_outbox {
        TEXT id PK
        TEXT user_id FK
        TEXT to_email
        TEXT subject
        TEXT body
        TEXT status "Pending | Sent | Failed"
        INTEGER attempts
        TEXT next_attempt_at
        TEXT last_error "nullable"
        TEXT created_at
        TEXT sent_at "nullable"
    }
```

## 3. Catalog Service (`catalog.db`)

```mermaid
erDiagram
    categories |o--o{ products : "contains"
    reservations ||--|{ reservation_items : "has"

    categories {
        TEXT id PK
        TEXT name "unique เฉพาะที่ยังไม่ถูกลบ"
        INTEGER is_deleted "0 | 1 (soft delete)"
        TEXT deleted_at "nullable"
        TEXT created_at
        TEXT updated_at
    }
    products {
        TEXT id PK
        TEXT category_id FK "nullable"
        TEXT name
        TEXT description "default ว่าง"
        INTEGER price_minor "> 0"
        INTEGER stock ">= 0"
        INTEGER is_deleted "0 | 1"
        TEXT deleted_at "nullable"
        TEXT created_at
        TEXT updated_at
    }
    reservations {
        TEXT order_id PK "logical ref to order.db"
        TEXT status "Reserved | Committed | Released"
        TEXT created_at
        TEXT updated_at
    }
    reservation_items {
        TEXT reservation_id PK, FK
        TEXT product_id PK "logical ref (no FK to products)"
        INTEGER quantity "> 0"
    }
    outbox {
        TEXT id PK "eventId"
        TEXT topic
        TEXT message_key
        TEXT payload
        TEXT correlation_id
        TEXT status "Pending | Sent"
        INTEGER attempts
        TEXT next_attempt_at
        TEXT created_at
        TEXT sent_at "nullable"
    }
    inbox {
        TEXT event_id PK
        TEXT topic
        TEXT processed_at
    }
```

ตาราง `outbox` และ `inbox` ไม่ผูกกับตารางอื่น ใช้เก็บ event ที่รอส่งและ event ที่ประมวลผลแล้วเพื่อกันการทำซ้ำ

การลบหมวดหมู่เป็น soft delete (`is_deleted`) และสินค้าในหมวดนั้นจะมี `category_id` เป็นค่าว่าง ไม่ถูกลบตาม

## 4. Order Service (`order.db`)

```mermaid
erDiagram
    orders ||--|{ order_items : "contains"
    orders ||--o| idempotency_keys : "created by"

    orders {
        TEXT id PK
        TEXT user_id "logical ref: JWT sub"
        TEXT owner_email "snapshot from JWT"
        TEXT status "Pending | Confirmed | Rejected | Cancelled"
        TEXT stock_status "Reserving | Reserved"
        INTEGER total_minor "nullable until Reserved"
        TEXT rejection_type "nullable: STOCK_UNAVAILABLE | TIMEOUT | ADMIN_REJECTED"
        TEXT rejection_message "nullable: เหตุผลที่ Admin ระบุ"
        TEXT recipient_name "delivery address, immutable"
        TEXT phone
        TEXT address_line
        TEXT province
        TEXT postal_code
        TEXT payment_method "CASH_ON_DELIVERY | PAY_AT_STORE"
        TEXT created_at
        TEXT updated_at
    }
    order_items {
        TEXT id PK
        TEXT order_id FK
        TEXT product_id "logical ref to catalog"
        INTEGER quantity "> 0"
        TEXT product_name "snapshot, nullable until Reserved"
        INTEGER unit_price_minor "snapshot, nullable until Reserved"
        TEXT failure_reason "nullable: INSUFFICIENT_STOCK | PRODUCT_NOT_FOUND"
        INTEGER failure_requested "nullable"
        INTEGER failure_available "nullable"
    }
    idempotency_keys {
        TEXT user_id PK
        TEXT idempotency_key PK
        TEXT order_id FK
        TEXT request_hash
        TEXT created_at
    }
    outbox {
        TEXT id PK "eventId"
        TEXT topic
        TEXT message_key
        TEXT payload
        TEXT correlation_id
        TEXT status "Pending | Sent"
        INTEGER attempts
        TEXT next_attempt_at
        TEXT created_at
        TEXT sent_at "nullable"
    }
    inbox {
        TEXT event_id PK
        TEXT topic
        TEXT processed_at
    }
```

คำสั่งซื้อที่ถูกปฏิเสธเก็บเหตุผลไว้ที่ `rejection_type` และ `rejection_message` ส่วนเหตุผลรายสินค้า (สต็อกไม่พอหรือไม่พบสินค้า) เก็บใน `order_items.failure_*`

ชื่อและราคาสินค้าใน `order_items` เป็นสำเนา ณ เวลาที่สั่ง จึงไม่เปลี่ยนตามการแก้ไขหรือลบสินค้าใน Catalog
