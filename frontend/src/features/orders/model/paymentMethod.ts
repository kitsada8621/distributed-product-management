import type { PaymentMethod } from '../types'

export const PAYMENT_METHOD_OPTIONS: { value: PaymentMethod; label: string }[] = [
  { value: 'CASH_ON_DELIVERY', label: 'เก็บเงินปลายทาง' },
  { value: 'PAY_AT_STORE', label: 'ชำระหน้าร้าน' },
]

export function paymentMethodLabel(value: PaymentMethod): string {
  const option = PAYMENT_METHOD_OPTIONS.find((candidate) => candidate.value === value)
  return option ? option.label : value
}
