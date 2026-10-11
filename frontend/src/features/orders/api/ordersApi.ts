import { apiRequest, apiRequestPage } from '@/shared/api/httpClient'
import { PAGE_SIZE } from '@/shared/config/env'
import type { CreateOrderInput, CreateOrderResult, Order, OrderRow } from '../types'

export function createOrder(input: CreateOrderInput, idempotencyKey: string) {
  return apiRequest<CreateOrderResult>('POST', '/api/orders', {
    body: input,
    headers: { 'Idempotency-Key': idempotencyKey },
  })
}

export function listMyOrders(input: { status: string; page: number }) {
  return apiRequestPage<OrderRow>('/api/orders', {
    query: { status: input.status, page: input.page, pageSize: PAGE_SIZE },
  })
}

export function getOrder(id: string) {
  return apiRequest<Order>('GET', `/api/orders/${id}`)
}

export function cancelOrder(id: string) {
  return apiRequest<Order>('POST', `/api/orders/${id}/cancel`)
}
