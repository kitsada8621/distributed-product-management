import { apiRequest, apiRequestPage } from '@/shared/api/httpClient'
import { PAGE_SIZE } from '@/shared/config/env'
import type { Product, ProductListQuery } from '../types'

export function fetchProducts({ q, categoryId, page }: ProductListQuery) {
  return apiRequestPage<Product>('/api/catalog/products', {
    query: { q, categoryId, page, pageSize: PAGE_SIZE },
  })
}

export function fetchProduct(id: string) {
  return apiRequest<Product>('GET', `/api/catalog/products/${id}`)
}

export function deleteProduct(id: string) {
  return apiRequest<null>('DELETE', `/api/catalog/products/${id}`)
}

export interface ProductPayload {
  name: string
  description: string
  price: number
  categoryId: string
}

export function createProduct(input: ProductPayload & { stock: number }) {
  return apiRequest<Product>('POST', '/api/catalog/products', { body: input })
}

export function updateProduct(id: string, input: ProductPayload) {
  return apiRequest<Product>('PUT', `/api/catalog/products/${id}`, { body: input })
}
