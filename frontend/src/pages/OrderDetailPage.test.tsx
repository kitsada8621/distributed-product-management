import { screen } from '@testing-library/react'
import { afterAll, afterEach, beforeAll, beforeEach, expect, it } from 'vitest'
import { server } from '@/mocks/server'
import { renderApp } from '@/test/renderApp'
import { seedOrder } from '@/test/seedOrder'
import { resetTestState, signIn } from '@/test/session'

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
beforeEach(async () => {
  resetTestState()
  await signIn('user')
})
afterEach(() => server.resetHandlers())
afterAll(() => server.close())

it('shows a reserved order waiting for the admin, with its snapshot items and total', async () => {
  const order = seedOrder()
  renderApp(`/orders/${order.id}`)

  expect(await screen.findByText('จองสต็อกแล้ว รอผู้ดูแลระบบยืนยัน')).toBeInTheDocument()
  expect(screen.getByText('รอการยืนยัน')).toBeInTheDocument()
  expect(screen.getByText('Mouse')).toBeInTheDocument()
  expect(screen.getByText(/ยอดรวม/)).toHaveTextContent('฿300.00')
})

it('shows progress and placeholders while the stock is still being reserved', async () => {
  const order = seedOrder({
    stockStatus: 'Reserving',
    total: null,
    items: [{ productId: 'p-2', name: null, unitPrice: null, quantity: 2 }],
  })
  renderApp(`/orders/${order.id}`)

  expect(await screen.findByText(/กำลังตรวจสอบสต็อกของคำสั่งซื้อ/)).toBeInTheDocument()
  expect(screen.getByText(/ยอดรวม/)).toHaveTextContent('ยอดรวม -')
})

it('shows why a rejected order was rejected', async () => {
  const order = seedOrder({
    status: 'Rejected',
    stockStatus: null,
    rejection: {
      type: 'STOCK_UNAVAILABLE',
      message: null,
      failedItems: [{ productId: 'p-2', reason: 'INSUFFICIENT_STOCK', requested: 2, available: 1 }],
    },
  })
  renderApp(`/orders/${order.id}`)

  expect(await screen.findByText('Mouse: ขอ 2 ชิ้น มีเพียง 1 ชิ้น')).toBeInTheDocument()
})

it('shows not found for an unknown order and for someone else’s order', async () => {
  const others = seedOrder({ userId: 'u-admin', ownerEmail: 'admin@example.com' })
  const first = renderApp(`/orders/${others.id}`)
  expect(await screen.findByText('ไม่พบคำสั่งซื้อ')).toBeInTheDocument()
  first.unmount()

  renderApp('/orders/missing')
  expect(await screen.findByText('ไม่พบคำสั่งซื้อ')).toBeInTheDocument()
})
