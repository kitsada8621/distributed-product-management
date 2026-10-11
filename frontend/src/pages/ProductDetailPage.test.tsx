import { screen } from '@testing-library/react'
import { afterAll, afterEach, beforeAll, beforeEach, expect, it } from 'vitest'
import { resetDb } from '@/mocks/db'
import { server } from '@/mocks/server'
import { renderApp } from '@/test/renderApp'

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
beforeEach(() => resetDb())
afterEach(() => server.resetHandlers())
afterAll(() => server.close())

it('shows the product details', async () => {
  renderApp('/products/p-2')

  expect(await screen.findByRole('heading', { name: 'Mouse' })).toBeInTheDocument()
  expect(screen.getByText('Mouse description')).toBeInTheDocument()
  expect(screen.getByText('Books')).toBeInTheDocument()
  expect(screen.getByText(/มีของ/)).toBeInTheDocument()
  expect(screen.getByText('฿150.00')).toBeInTheDocument()
})

it('shows a not found view for an unknown product', async () => {
  renderApp('/products/missing')

  expect(await screen.findByText('ไม่พบสินค้า')).toBeInTheDocument()
  expect(screen.getByRole('link', { name: 'กลับไปหน้าสินค้า' })).toHaveAttribute('href', '/products')
})
