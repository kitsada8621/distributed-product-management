import { apiRequest } from '@/shared/api/httpClient'

export function setStock(productId: string, quantity: number) {
  return apiRequest<{ stock: number }>('PUT', `/api/catalog/products/${productId}/stock`, { body: { quantity } })
}
