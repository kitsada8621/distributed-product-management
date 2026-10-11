import { db, type MockOrder } from '@/mocks/db'

export function seedOrder(overrides: Partial<MockOrder> = {}): MockOrder {
  const now = new Date().toISOString()
  const order: MockOrder = {
    id: crypto.randomUUID(),
    userId: 'u-user',
    ownerEmail: 'user@example.com',
    idempotencyKey: crypto.randomUUID(),
    requestBody: '',
    status: 'Pending',
    stockStatus: 'Reserved',
    items: [{ productId: 'p-2', name: 'Mouse', unitPrice: 150, quantity: 2 }],
    total: 300,
    shippingAddress: {
      recipientName: 'Somchai Jaidee',
      phone: '0812345678',
      addressLine: '99/9 Soi Sukhumvit 21',
      province: 'Bangkok',
      postalCode: '10110',
    },
    paymentMethod: 'CASH_ON_DELIVERY',
    rejection: null,
    createdAt: now,
    updatedAt: now,
    ...overrides,
  }
  db.orders.push(order)
  return order
}
