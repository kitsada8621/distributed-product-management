import { render, screen } from '@testing-library/react'
import { afterAll, afterEach, beforeAll, expect, it } from 'vitest'
import { server } from '@/mocks/server'
import { createQueryWrapper } from '@/test/queryWrapper'
import type { OrderItem, Rejection } from '../types'
import { RejectionReasonList } from './RejectionReasonList'

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
afterEach(() => server.resetHandlers())
afterAll(() => server.close())

const items: OrderItem[] = [{ productId: 'p1', name: 'Keyboard', unitPrice: 100, quantity: 5 }]

function renderRejection(rejection: Rejection) {
  const { wrapper } = createQueryWrapper()
  render(<RejectionReasonList rejection={rejection} items={items} />, { wrapper })
}

it('shows requested and available for INSUFFICIENT_STOCK', () => {
  renderRejection({
    type: 'STOCK_UNAVAILABLE',
    message: null,
    failedItems: [{ productId: 'p1', reason: 'INSUFFICIENT_STOCK', requested: 5, available: 2 }],
  })

  expect(screen.getByText('Keyboard: ขอ 5 ชิ้น มีเพียง 2 ชิ้น')).toBeInTheDocument()
})

it('says a PRODUCT_NOT_FOUND product is no longer available, falling back to the id', () => {
  renderRejection({
    type: 'STOCK_UNAVAILABLE',
    message: null,
    failedItems: [{ productId: 'gone', reason: 'PRODUCT_NOT_FOUND', requested: 1, available: 0 }],
  })

  expect(screen.getByText('gone: ไม่พบสินค้า')).toBeInTheDocument()
})

it('explains a TIMEOUT', () => {
  renderRejection({ type: 'TIMEOUT', message: null, failedItems: [] })

  expect(screen.getByText(/ตรวจสอบสต็อกไม่ทันเวลา/)).toBeInTheDocument()
})

it('shows the admin reason for ADMIN_REJECTED', () => {
  renderRejection({ type: 'ADMIN_REJECTED', message: 'Item damaged', failedItems: [] })

  expect(screen.getByText('ผู้ดูแลระบบปฏิเสธ: Item damaged')).toBeInTheDocument()
})

it('looks the product name up when the order has no name snapshot', async () => {
  const { wrapper } = createQueryWrapper()
  render(
    <RejectionReasonList
      rejection={{
        type: 'STOCK_UNAVAILABLE',
        message: null,
        failedItems: [{ productId: 'p-3', reason: 'INSUFFICIENT_STOCK', requested: 99, available: 7 }],
      }}
      items={[{ productId: 'p-3', name: null, unitPrice: null, quantity: 99 }]}
    />,
    { wrapper },
  )

  expect(await screen.findByText('Monitor: ขอ 99 ชิ้น มีเพียง 7 ชิ้น')).toBeInTheDocument()
})
