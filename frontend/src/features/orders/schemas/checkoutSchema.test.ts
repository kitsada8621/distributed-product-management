import { describe, expect, it } from 'vitest'
import { checkoutSchema } from './checkoutSchema'

const validInput = {
  shippingAddress: {
    recipientName: 'Somchai Jaidee',
    phone: '0812345678',
    addressLine: '99/9 Soi Sukhumvit 21',
    province: 'Bangkok',
    postalCode: '10110',
  },
  paymentMethod: 'CASH_ON_DELIVERY',
}

function failedPaths(input: unknown): string[] {
  const result = checkoutSchema.safeParse(input)
  if (result.success) return []
  return result.error.issues.map((issue) => issue.path.join('.'))
}

function withAddress(change: Record<string, string>) {
  return { ...validInput, shippingAddress: { ...validInput.shippingAddress, ...change } }
}

describe('checkoutSchema', () => {
  it('accepts a valid input for both payment methods', () => {
    expect(checkoutSchema.safeParse(validInput).success).toBe(true)
    expect(checkoutSchema.safeParse({ ...validInput, paymentMethod: 'PAY_AT_STORE' }).success).toBe(true)
  })

  it('trims the strings', () => {
    const result = checkoutSchema.parse(withAddress({ recipientName: '  Somchai  ', postalCode: ' 10110 ' }))

    expect(result.shippingAddress.recipientName).toBe('Somchai')
    expect(result.shippingAddress.postalCode).toBe('10110')
  })

  it.each(['recipientName', 'phone', 'addressLine', 'province', 'postalCode'])('rejects a blank %s', (field) => {
    expect(failedPaths(withAddress({ [field]: '   ' }))).toEqual([`shippingAddress.${field}`])
  })

  it.each([
    ['recipientName', 101],
    ['addressLine', 256],
    ['province', 101],
  ])('rejects a too long %s', (field, length) => {
    expect(failedPaths(withAddress({ [field]: 'a'.repeat(length) }))).toEqual([`shippingAddress.${field}`])
  })

  it.each(['812345678', '12345', '08123456789', '08123abcde'])('rejects the phone %s', (phone) => {
    expect(failedPaths(withAddress({ phone }))).toEqual(['shippingAddress.phone'])
  })

  it.each(['1011', '101101', '1011a'])('rejects the postal code %s', (postalCode) => {
    expect(failedPaths(withAddress({ postalCode }))).toEqual(['shippingAddress.postalCode'])
  })

  it.each([undefined, '', 'BITCOIN'])('rejects the payment method %s', (paymentMethod) => {
    expect(failedPaths({ ...validInput, paymentMethod })).toEqual(['paymentMethod'])
  })
})
