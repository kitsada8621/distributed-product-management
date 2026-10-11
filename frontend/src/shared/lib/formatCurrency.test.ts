import { expect, it } from 'vitest'
import { formatCurrency } from './formatCurrency'

it('formats a price in baht with two decimals', () => {
  expect(formatCurrency(1290)).toBe('฿1,290.00')
})

it('formats zero', () => {
  expect(formatCurrency(0)).toBe('฿0.00')
})
