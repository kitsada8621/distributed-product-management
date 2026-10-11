import { apiRequest, apiRequestPage } from '@/shared/api/httpClient'
import { PAGE_SIZE } from '@/shared/config/env'
import type { Order, OrderRow } from '@/features/orders'

export function listAllOrders(input: { status: string; page: number }) {
  return apiRequestPage<OrderRow>('/api/orders/admin', {
    query: { status: input.status, page: input.page, pageSize: PAGE_SIZE },
  })
}

export function getAdminOrder(id: string) {
  return apiRequest<Order>('GET', `/api/orders/admin/${id}`)
}

export function confirmOrder(id: string) {
  return apiRequest<Order>('POST', `/api/orders/${id}/confirm`)
}

export function rejectOrder(id: string, reason: string) {
  return apiRequest<Order>('POST', `/api/orders/${id}/reject`, { body: { reason } })
}
