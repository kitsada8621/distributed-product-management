import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { expect, it } from 'vitest'
import type { Product } from '../types'
import { ProductCard } from './ProductCard'

const product: Product = {
  id: 'p1',
  name: 'Keyboard',
  description: '',
  price: 1290,
  categoryId: 'c1',
  categoryName: 'Accessories',
  inStock: true,
  stock: null,
}

function renderCard(overrides: Partial<Product>) {
  render(
    <MemoryRouter>
      <ProductCard product={{ ...product, ...overrides }} />
    </MemoryRouter>,
  )
}

it('shows ไม่มีหมวดหมู่ and สินค้าหมด for a product whose category was deleted', () => {
  renderCard({ categoryId: null, categoryName: null, inStock: false })

  expect(screen.getByText('ไม่มีหมวดหมู่')).toBeInTheDocument()
  expect(screen.getByText('สินค้าหมด')).toBeInTheDocument()
})

it('shows the category name when the product has one', () => {
  renderCard({})

  expect(screen.getByText('Accessories')).toBeInTheDocument()
  expect(screen.queryByText('ไม่มีหมวดหมู่')).not.toBeInTheDocument()
})

it('shows the out-of-stock badge for a product with no stock', () => {
  renderCard({ inStock: false })

  expect(screen.getByText('สินค้าหมด')).toBeInTheDocument()
})
