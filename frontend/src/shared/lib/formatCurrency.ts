const CURRENCY = 'THB'

const formatter = new Intl.NumberFormat('th-TH', {
  style: 'currency',
  currency: CURRENCY,
})

export function formatCurrency(amount: number): string {
  return formatter.format(amount)
}
