import { apiRequest } from '@/shared/api/httpClient'
import type { Category } from '../types'

export function fetchCategories() {
  return apiRequest<Category[]>('GET', '/api/catalog/categories')
}

export function createCategory(name: string) {
  return apiRequest<Category>('POST', '/api/catalog/categories', { body: { name } })
}

export function updateCategory(id: string, name: string) {
  return apiRequest<Category>('PUT', `/api/catalog/categories/${id}`, { body: { name } })
}

export function deleteCategory(id: string) {
  return apiRequest<null>('DELETE', `/api/catalog/categories/${id}`)
}
