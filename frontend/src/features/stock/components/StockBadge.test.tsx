import { render, screen } from '@testing-library/react'
import { expect, it } from 'vitest'
import { StockBadge } from './StockBadge'

it('shows the number when the response carries one', () => {
  render(<StockBadge inStock stock={25} />)

  expect(screen.getByText('มีของ · 25 ชิ้น')).toBeInTheDocument()
})

it('shows only availability when stock is undefined or null', () => {
  const { unmount } = render(<StockBadge inStock />)
  expect(screen.getByText('มีของ')).toBeInTheDocument()
  unmount()

  render(<StockBadge inStock stock={null} />)
  expect(screen.getByText('มีของ')).toBeInTheDocument()
  expect(screen.queryByText(/ชิ้น/)).not.toBeInTheDocument()
})

it('shows out of stock with an icon when inStock is false', () => {
  render(<StockBadge inStock={false} stock={0} />)

  expect(screen.getByText('สินค้าหมด')).toBeInTheDocument()
  expect(screen.getByTestId('CancelIcon')).toBeInTheDocument()
})

it('shows out of stock for stock 0 even without other data', () => {
  render(<StockBadge inStock={false} stock={0} />)

  expect(screen.getByText('สินค้าหมด')).toBeInTheDocument()
  expect(screen.queryByText(/มีของ/)).not.toBeInTheDocument()
})
