import { renderHook } from '@testing-library/react'
import { expect, it } from 'vitest'
import type { DraftItem } from '../model/orderDraftStore'
import { useIdempotencyKey, type DeliveryValues } from './useIdempotencyKey'

const item = (quantity: number): DraftItem => ({ productId: 'p1', name: 'Keyboard', unitPrice: 100, quantity })

const delivery = (overrides: { addressLine?: string; paymentMethod?: 'CASH_ON_DELIVERY' | 'PAY_AT_STORE' } = {}): DeliveryValues => ({
  shippingAddress: {
    recipientName: 'Somchai Jaidee',
    phone: '0812345678',
    addressLine: overrides.addressLine ?? '99/9 Soi Sukhumvit 21',
    province: 'Bangkok',
    postalCode: '10110',
  },
  paymentMethod: overrides.paymentMethod ?? 'CASH_ON_DELIVERY',
})

function renderKey(items: DraftItem[], values: DeliveryValues) {
  return renderHook((props) => useIdempotencyKey(props.items, props.values), {
    initialProps: { items, values },
  })
}

it('keeps the same key while the draft and delivery are unchanged', () => {
  const { result, rerender } = renderKey([item(1)], delivery())
  const first = result.current

  rerender({ items: [item(1)], values: delivery() })

  expect(result.current).toBe(first)
})

it('renews the key after the draft is edited', () => {
  const { result, rerender } = renderKey([item(1)], delivery())
  const first = result.current

  rerender({ items: [item(2)], values: delivery() })

  expect(result.current).not.toBe(first)
})

it('renews the key after the address line is edited', () => {
  const { result, rerender } = renderKey([item(1)], delivery())
  const first = result.current

  rerender({ items: [item(1)], values: delivery({ addressLine: '1 Another Road' }) })

  expect(result.current).not.toBe(first)
})

it('renews the key after the payment method is changed', () => {
  const { result, rerender } = renderKey([item(1)], delivery())
  const first = result.current

  rerender({ items: [item(1)], values: delivery({ paymentMethod: 'PAY_AT_STORE' }) })

  expect(result.current).not.toBe(first)
})
