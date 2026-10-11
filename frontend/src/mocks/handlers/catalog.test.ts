import { afterAll, afterEach, beforeAll, beforeEach, expect, it } from 'vitest'
import { apiRequest, apiRequestPage, setAccessTokenGetter } from '@/shared/api/httpClient'
import type { ApiError } from '@/shared/api/types'
import { resetDb } from '../db'
import { server } from '../server'

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
beforeEach(() => resetDb())
afterEach(() => {
  server.resetHandlers()
  setAccessTokenGetter(() => null)
})
afterAll(() => server.close())

async function loginAs(email: string, password: string) {
  const { accessToken } = await apiRequest<{ accessToken: string }>('POST', '/api/auth/login', {
    body: { email, password },
  })
  setAccessTokenGetter(() => accessToken)
}

const loginAdmin = () => loginAs('admin@example.com', 'Admin123!')
const errorOf = async (promise: Promise<unknown>) => (await promise.catch((e) => e)) as ApiError

interface Product {
  id: string
  name: string
  categoryId: string
  inStock: boolean
  stock: number | null
}

it('answers inStock true above zero stock and false at zero', async () => {
  await loginAdmin()
  const page = await apiRequestPage<Product>('/api/catalog/products', { query: { pageSize: 100 } })

  expect(page.items.filter((p) => p.stock! > 0).every((p) => p.inStock)).toBe(true)
  const soldOut = page.items.filter((p) => p.stock === 0)
  expect(soldOut.length).toBeGreaterThan(0)
  expect(soldOut.every((p) => !p.inStock)).toBe(true)
})

it('hides the stock number from a guest and gives it to an admin', async () => {
  const guest = await apiRequestPage<Product>('/api/catalog/products')
  await loginAdmin()
  const admin = await apiRequestPage<Product>('/api/catalog/products')

  expect(guest.items.every((p) => p.stock === null && typeof p.inStock === 'boolean')).toBe(true)
  expect(admin.items.every((p) => typeof p.stock === 'number')).toBe(true)
})

it('searches by name, case-insensitive contains', async () => {
  const page = await apiRequestPage<Product>('/api/catalog/products', { query: { q: 'MOUSE' } })

  expect(page.items.map((p) => p.name)).toEqual(['Mouse', 'Mouse Pad'])
})

it('filters by category', async () => {
  const page = await apiRequestPage<Product>('/api/catalog/products', { query: { categoryId: 'c-books' } })

  expect(page.items.length).toBeGreaterThan(0)
  expect(page.items.every((p) => p.categoryId === 'c-books')).toBe(true)
})

it('paginates sorted by name with default 20 per page', async () => {
  const first = await apiRequestPage<Product>('/api/catalog/products')
  const second = await apiRequestPage<Product>('/api/catalog/products', { query: { page: 2 } })

  expect(first.items).toHaveLength(20)
  expect(first.total).toBe(24)
  expect(first.items.every((p) => typeof p.inStock === 'boolean')).toBe(true)
  expect(second.items).toHaveLength(4)
  expect(first.items[0].name < first.items[1].name).toBe(true)
})

it('returns 404 for an unknown product', async () => {
  const error = await errorOf(apiRequest('GET', '/api/catalog/products/nope'))

  expect(error.status).toBe(404)
})

it('soft deletes a product so it disappears from list and detail', async () => {
  await loginAdmin()
  await apiRequest('DELETE', '/api/catalog/products/p-2')

  const page = await apiRequestPage<Product>('/api/catalog/products', { query: { pageSize: 100 } })
  const error = await errorOf(apiRequest('GET', '/api/catalog/products/p-2'))

  expect(page.items.some((p) => p.id === 'p-2')).toBe(false)
  expect(error.status).toBe(404)
})

it('deletes a category and keeps its products with no category', async () => {
  await loginAdmin()

  await apiRequest('DELETE', '/api/catalog/categories/c-books')
  const categories = await apiRequest<{ id: string }[]>('GET', '/api/catalog/categories')
  const page = await apiRequestPage<Product & { categoryName: string | null }>('/api/catalog/products', {
    query: { pageSize: 100 },
  })

  expect(categories.some((c) => c.id === 'c-books')).toBe(false)
  const orphans = page.items.filter((p) => p.categoryId === null)
  expect(orphans.length).toBeGreaterThan(0)
  expect(orphans.every((p) => p.categoryName === null)).toBe(true)
})

it('sets stock and rejects a negative value with STOCK_NEGATIVE', async () => {
  await loginAdmin()

  const result = await apiRequest<{ stock: number }>('PUT', '/api/catalog/products/p-2/stock', {
    body: { quantity: 40 },
  })
  const error = await errorOf(apiRequest('PUT', '/api/catalog/products/p-2/stock', { body: { quantity: -1 } }))

  expect(result.stock).toBe(40)
  expect(error.status).toBe(422)
  expect(error.code).toBe('STOCK_NEGATIVE')
})

it('validates a new product', async () => {
  await loginAdmin()

  const error = await errorOf(
    apiRequest('POST', '/api/catalog/products', { body: { name: '', price: 0, categoryId: 'x', stock: -1 } }),
  )

  expect(error.status).toBe(422)
  expect(Object.keys(error.fieldErrors).sort()).toEqual(['categoryId', 'name', 'price', 'stock'])
})

it('creates a product that returns categoryName', async () => {
  await loginAdmin()

  const created = await apiRequest<{ categoryName: string; inStock: boolean }>('POST', '/api/catalog/products', {
    body: { name: 'New', price: 10, categoryId: 'c-books', stock: 3 },
  })

  expect(created.categoryName).toBe('Books')
  expect(created.inStock).toBe(true)
})

it('rejects admin endpoints without a token (401) and for a user (403)', async () => {
  const anonymous = await errorOf(apiRequest('POST', '/api/catalog/categories', { body: { name: 'X' } }))
  await loginAs('user@example.com', 'Passw0rd!')
  const user = await errorOf(apiRequest('POST', '/api/catalog/categories', { body: { name: 'X' } }))

  expect(anonymous.status).toBe(401)
  expect(user.status).toBe(403)
})
