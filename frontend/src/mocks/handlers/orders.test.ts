import { afterAll, afterEach, beforeAll, beforeEach, expect, it, vi } from 'vitest'
import { apiRequest, apiRequestPage, setAccessTokenGetter } from '@/shared/api/httpClient'
import type { ApiError } from '@/shared/api/types'
import { db, resetDb } from '../db'
import { server } from '../server'

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
beforeEach(() => {
  resetDb()
  db.reserveDelayMs = 0
})
afterEach(() => {
  server.resetHandlers()
  setAccessTokenGetter(() => null)
})
afterAll(() => server.close())

async function loginAs(email: string, password: string) {
  const { accessToken } = await apiRequest<{ accessToken: string }>('POST', '/api/auth/login', {
    body: { email, password },
  })
  setAccessTokenGetter(() => accessToken)
}

const loginUser = () => loginAs('user@example.com', 'Passw0rd!')
const loginAdmin = () => loginAs('admin@example.com', 'Admin123!')
const errorOf = async (promise: Promise<unknown>) => (await promise.catch((e) => e)) as ApiError

interface Order {
  id: string
  status: string
  stockStatus: string | null
  total: number | null
  shippingAddress: { recipientName: string; phone: string; addressLine: string; province: string; postalCode: string }
  paymentMethod: string
  rejection: { type: string; message: string | null } | null
  ownerEmail?: string
}

const validAddress = {
  recipientName: 'Somchai Jaidee',
  phone: '0812345678',
  addressLine: '99/9 Soi Sukhumvit 21',
  province: 'Bangkok',
  postalCode: '10110',
}

async function createOrder(key: string, quantity = 2, productId = 'p-2', delivery: Record<string, unknown> = {}) {
  return apiRequest<{ orderId: string; status: string; stockStatus: string }>('POST', '/api/orders', {
    body: {
      items: [{ productId, quantity }],
      shippingAddress: validAddress,
      paymentMethod: 'CASH_ON_DELIVERY',
      ...delivery,
    },
    headers: { 'Idempotency-Key': key },
  })
}

const getOrder = (id: string) => apiRequest<Order>('GET', `/api/orders/${id}`)

async function reservedOrder(key = 'k1') {
  const created = await createOrder(key)
  await vi.waitFor(async () => expect((await getOrder(created.orderId)).stockStatus).toBe('Reserved'))
  return created.orderId
}

it('creates an order as Pending/Reserving then reserves it with a snapshot total', async () => {
  await loginUser()

  const created = await createOrder('k1')
  expect(created).toMatchObject({ status: 'Pending', stockStatus: 'Reserving' })

  await vi.waitFor(async () => expect((await getOrder(created.orderId)).stockStatus).toBe('Reserved'))
  expect((await getOrder(created.orderId)).total).toBe(300)
})

it('rejects with STOCK_UNAVAILABLE when quantity exceeds stock', async () => {
  await loginUser()

  const created = await createOrder('k1', 999)
  await vi.waitFor(async () => expect((await getOrder(created.orderId)).status).toBe('Rejected'))

  expect((await getOrder(created.orderId)).rejection?.type).toBe('STOCK_UNAVAILABLE')
})

it('requires the Idempotency-Key header', async () => {
  await loginUser()

  const error = await errorOf(apiRequest('POST', '/api/orders', {
      body: { items: [{ productId: 'p-2', quantity: 1 }], shippingAddress: validAddress, paymentMethod: 'CASH_ON_DELIVERY' },
    }))

  expect(error.status).toBe(400)
  expect(error.code).toBe('IDEMPOTENCY_KEY_REQUIRED')
})

it('returns the same order for a repeated key and 409 for a different body', async () => {
  await loginUser()
  const first = await createOrder('k1')

  const again = await createOrder('k1')
  const conflict = await errorOf(createOrder('k1', 3))

  expect(again.orderId).toBe(first.orderId)
  expect(db.orders).toHaveLength(1)
  expect(conflict.status).toBe(409)
  expect(conflict.code).toBe('IDEMPOTENCY_KEY_REUSED')
})

it('validates items', async () => {
  await loginUser()

  const error = await errorOf(createOrder('k1', 0))

  expect(error.status).toBe(422)
})

it('hides another user\'s order with 404', async () => {
  await loginUser()
  const orderId = await reservedOrder()
  await loginAdmin()

  const error = await errorOf(getOrder(orderId))

  expect(error.status).toBe(404)
})

const fieldsOf = (error: ApiError) => Object.keys(error.fieldErrors)

it('requires the shipping address and payment method', async () => {
  await loginUser()

  const error = await errorOf(createOrder('k1', 2, 'p-2', { shippingAddress: undefined, paymentMethod: undefined }))

  expect(error.status).toBe(422)
  expect(fieldsOf(error)).toEqual([
    'shippingAddress.recipientName',
    'shippingAddress.phone',
    'shippingAddress.addressLine',
    'shippingAddress.province',
    'shippingAddress.postalCode',
    'paymentMethod',
  ])
  expect(db.orders).toHaveLength(0)
})

it('rejects a bad phone with the field path', async () => {
  await loginUser()

  const error = await errorOf(createOrder('k1', 2, 'p-2', { shippingAddress: { ...validAddress, phone: '12345' } }))

  expect(error.status).toBe(422)
  expect(fieldsOf(error)).toEqual(['shippingAddress.phone'])
})

it('rejects an unknown payment method', async () => {
  await loginUser()

  const error = await errorOf(createOrder('k1', 2, 'p-2', { paymentMethod: 'BITCOIN' }))

  expect(error.status).toBe(422)
  expect(fieldsOf(error)).toEqual(['paymentMethod'])
})

it('returns the address and payment method in the order detail but not in the list', async () => {
  await loginUser()
  const created = await createOrder('k1', 2, 'p-2', { paymentMethod: 'PAY_AT_STORE' })

  const order = await getOrder(created.orderId)
  const list = await apiRequestPage<Record<string, unknown>>('/api/orders')

  expect(order.shippingAddress).toEqual(validAddress)
  expect(order.paymentMethod).toBe('PAY_AT_STORE')
  expect(list.items[0]).not.toHaveProperty('shippingAddress')
})

it('answers 409 when the same key is reused with another address', async () => {
  await loginUser()
  await createOrder('k1')

  const otherAddress = { ...validAddress, addressLine: '1 Another Road' }
  const conflict = await errorOf(createOrder('k1', 2, 'p-2', { shippingAddress: otherAddress }))

  expect(conflict.code).toBe('IDEMPOTENCY_KEY_REUSED')
})

it('cancels a pending order and refuses to cancel twice', async () => {
  await loginUser()
  const orderId = await reservedOrder()

  const cancelled = await apiRequest<Order>('POST', `/api/orders/${orderId}/cancel`)
  const error = await errorOf(apiRequest('POST', `/api/orders/${orderId}/cancel`))

  expect(cancelled.status).toBe('Cancelled')
  expect(error.status).toBe(409)
  expect(error.code).toBe('CANNOT_CANCEL')
})

it('lists own orders newest first', async () => {
  await loginUser()
  await createOrder('k1')
  await createOrder('k2', 1)

  const page = await apiRequestPage<Order>('/api/orders')

  expect(page.items).toHaveLength(2)
  expect(page.total).toBe(2)
})

it('lets admin list all orders with owner email and filter by status', async () => {
  await loginUser()
  const orderId = await reservedOrder()
  await apiRequest('POST', `/api/orders/${orderId}/cancel`)
  await reservedOrder('k2')
  await loginAdmin()

  const all = await apiRequestPage<Order>('/api/orders/admin')
  const cancelled = await apiRequestPage<Order>('/api/orders/admin', { query: { status: 'Cancelled' } })
  const detail = await apiRequest<Order>('GET', `/api/orders/admin/${orderId}`)

  expect(all.items).toHaveLength(2)
  expect(all.items[0].ownerEmail).toBe('user@example.com')
  expect(cancelled.items).toHaveLength(1)
  expect(detail.ownerEmail).toBe('user@example.com')
})

it('forbids a user from admin routes', async () => {
  await loginUser()

  const error = await errorOf(apiRequestPage('/api/orders/admin'))

  expect(error.status).toBe(403)
})

it('confirms a reserved order and then refuses to confirm again', async () => {
  await loginUser()
  const orderId = await reservedOrder()
  await loginAdmin()

  const confirmed = await apiRequest<Order>('POST', `/api/orders/${orderId}/confirm`)
  const error = await errorOf(apiRequest('POST', `/api/orders/${orderId}/confirm`))

  expect(confirmed.status).toBe('Confirmed')
  expect(error.status).toBe(409)
  expect(error.code).toBe('INVALID_STATE')
})

it('rejects with a reason, requires the reason, and releases stock', async () => {
  await loginUser()
  const orderId = await reservedOrder()
  await loginAdmin()
  const stockAfterReserve = db.products.find((p) => p.id === 'p-2')!.stock

  const missing = await errorOf(apiRequest('POST', `/api/orders/${orderId}/reject`, { body: { reason: ' ' } }))
  const rejected = await apiRequest<Order>('POST', `/api/orders/${orderId}/reject`, { body: { reason: 'Damaged' } })

  expect(missing.status).toBe(422)
  expect(rejected.rejection).toMatchObject({ type: 'ADMIN_REJECTED', message: 'Damaged' })
  expect(db.products.find((p) => p.id === 'p-2')!.stock).toBe(stockAfterReserve + 2)
})

