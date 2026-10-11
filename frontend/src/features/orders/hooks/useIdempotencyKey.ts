import { useMemo } from 'react'
import { uuid } from '@/shared/lib/uuid'
import type { DraftItem } from '../model/orderDraftStore'
import type { PaymentMethod, ShippingAddress } from '../types'

export interface DeliveryValues {
  shippingAddress?: Partial<ShippingAddress>
  paymentMethod?: PaymentMethod | ''
}

export function useIdempotencyKey(items: DraftItem[], delivery: DeliveryValues): string {
  const address = delivery.shippingAddress ?? {}
  const itemsSignature = items.map((item) => `${item.productId}:${item.quantity}`).join('|')
  const deliverySignature = [
    address.recipientName,
    address.phone,
    address.addressLine,
    address.province,
    address.postalCode,
    delivery.paymentMethod,
  ].join('|')
  const signature = `${itemsSignature}#${deliverySignature}`
  // eslint-disable-next-line react-hooks/exhaustive-deps
  return useMemo(() => uuid(), [signature])
}
