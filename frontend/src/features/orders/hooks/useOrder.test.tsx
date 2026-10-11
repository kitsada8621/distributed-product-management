import { renderHook, waitFor } from '@testing-library/react'
import { afterAll, afterEach, beforeAll, beforeEach, expect, it } from 'vitest'
import { db } from '@/mocks/db'
import { server } from '@/mocks/server'
import { createQueryWrapper } from '@/test/queryWrapper'
import { resetTestState, signIn } from '@/test/session'
import { createOrder } from '../api/ordersApi'
import { useOrder } from './useOrder'

let orderFetches = 0

beforeAll(() => {
  server.listen({ onUnhandledRequest: 'error' })
  server.events.on('request:start', ({ request }) => {
    if (request.method === 'GET' && /\/api\/orders\/[^/]+$/.test(request.url)) orderFetches += 1
  })
})
beforeEach(async () => {
  resetTestState()
  orderFetches = 0
  await signIn('user')
})
afterEach(() => server.resetHandlers())
afterAll(() => server.close())

const shippingAddress = {
  recipientName: 'Somchai Jaidee',
  phone: '0812345678',
  addressLine: '99/9 Soi Sukhumvit 21',
  province: 'Bangkok',
  postalCode: '10110',
}

const newOrder = () =>
  createOrder(
    { items: [{ productId: 'p-2', quantity: 1 }], shippingAddress, paymentMethod: 'CASH_ON_DELIVERY' },
    crypto.randomUUID(),
  )

it('polls while Reserving and stops once the order is Reserved', async () => {
  db.reserveDelayMs = 150
  const { orderId } = await newOrder()
  const { wrapper } = createQueryWrapper()

  const { result } = renderHook(() => useOrder(orderId, { intervalMs: 30, timeoutMs: 5000 }), { wrapper })

  await waitFor(() => expect(result.current.data?.stockStatus).toBe('Reserved'))
  const fetchesAtReserved = orderFetches
  expect(fetchesAtReserved).toBeGreaterThan(2)
  await new Promise((resolve) => setTimeout(resolve, 150))
  expect(orderFetches).toBe(fetchesAtReserved)
})

it('gives up after the timeout and reports it', async () => {
  db.reserveDelayMs = 60_000
  const { orderId } = await newOrder()
  const { wrapper } = createQueryWrapper()

  const { result } = renderHook(() => useOrder(orderId, { intervalMs: 20, timeoutMs: 200 }), { wrapper })

  await waitFor(() => expect(result.current.pollingGaveUp).toBe(true))
  const fetchesAtTimeout = orderFetches
  await new Promise((resolve) => setTimeout(resolve, 120))
  expect(orderFetches).toBe(fetchesAtTimeout)
  expect(result.current.data?.stockStatus).toBe('Reserving')
})
