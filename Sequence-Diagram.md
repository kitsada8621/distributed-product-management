# Sequence Diagram

แผนภาพนี้แสดงเส้นทางหลักของระบบ ตั้งแต่เข้าสู่ระบบ ดูสินค้า สั่งซื้อ จนถึง Admin ยืนยันคำสั่งซื้อ

```mermaid
sequenceDiagram
    actor U as ผู้ใช้
    actor AD as Admin
    participant K as Kong
    participant A as Auth Service
    participant C as Catalog Service
    participant O as Order Service
    participant KF as Kafka

    Note over U,A: 1. เข้าสู่ระบบ
    U->>K: POST /api/auth/login
    K->>A: ส่งต่อคำขอ
    A-->>U: access token (JWT) และ refresh token

    Note over U,C: 2. ดูสินค้า (ไม่ต้องเข้าสู่ระบบก็ได้)
    U->>K: GET /api/catalog/products
    K->>C: ส่งต่อคำขอ
    C-->>U: รายการสินค้า

    Note over U,KF: 3. สร้างคำสั่งซื้อ
    U->>K: POST /api/orders (Bearer token + Idempotency-Key)
    K->>O: ส่งต่อคำขอ
    O->>O: ตรวจ token ด้วย public key, บันทึกคำสั่งซื้อสถานะ Pending
    O-->>U: 202 Accepted (ได้เลขคำสั่งซื้อ)
    O->>KF: order.created
    KF->>C: ส่ง event ให้ Catalog

    Note over C,O: 4. ตัดสต็อกและตอบผล
    alt สต็อกเพียงพอทุกรายการ
        C->>C: ตัดสต็อกและบันทึกการจอง
        C->>KF: stock.reserved (ชื่อและราคาสินค้า)
        KF->>O: ส่ง event ให้ Order
        O->>O: บันทึกราคาและยอดรวม
    else สต็อกไม่พอหรือไม่พบสินค้า
        C->>KF: stock.rejected (เหตุผลรายสินค้า)
        KF->>O: ส่ง event ให้ Order
        O->>O: เปลี่ยนสถานะเป็น Rejected
    end

    loop ทุก 2 วินาที
        U->>K: GET /api/orders/{id}
        K->>O: ส่งต่อคำขอ
        O-->>U: สถานะปัจจุบัน
    end

    Note over AD,C: 5. Admin ตัดสินใจ
    AD->>K: POST /api/orders/{id}/confirm
    K->>O: ส่งต่อคำขอ
    O->>O: เปลี่ยนสถานะเป็น Confirmed
    O->>KF: order.confirmed
    KF->>C: ยืนยันการตัดสต็อกถาวร

    Note over U,C: กรณียกเลิกหรือถูกปฏิเสธ: Order ส่ง order.cancelled และ Catalog คืนสต็อก
```

## คำอธิบายขั้นตอน

1. ผู้ใช้เข้าสู่ระบบกับ Auth Service และได้ token สำหรับใช้เรียกบริการอื่น
2. ผู้ใช้ดูสินค้าจาก Catalog ได้โดยไม่ต้องเข้าสู่ระบบ
3. เมื่อสั่งซื้อ Order Service บันทึกคำสั่งซื้อแล้วตอบกลับทันทีว่ารับเรื่องแล้ว (202) โดยยังไม่รู้ราคาหรือสต็อก เพราะไม่ได้เรียก Catalog โดยตรง
4. Catalog ได้รับ event ไปตัดสต็อก ถ้าสินค้าครบทุกรายการจึงสำเร็จ ถ้ารายการใดไม่พอ ทั้งคำสั่งซื้อจะถูกปฏิเสธ (all-or-nothing) หน้าเว็บสอบถามสถานะทุก 2 วินาทีจนกว่าจะได้ผล
5. Admin ยืนยันหรือปฏิเสธคำสั่งซื้อที่จองสต็อกสำเร็จแล้ว หากถูกปฏิเสธหรือผู้ใช้ยกเลิก ระบบคืนสต็อกให้อัตโนมัติ และหากไม่ได้รับผลจาก Catalog ภายใน 60 วินาที ระบบจะปฏิเสธคำสั่งซื้อนั้นเองและคืนสต็อก
