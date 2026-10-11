import type { OrderStatus, StockStatus } from '../types'

interface OrderState {
  status: OrderStatus
  stockStatus: StockStatus | null
}

export function isTerminal(order: OrderState): boolean {
  return order.status !== 'Pending'
}

export function canCancel(order: OrderState): boolean {
  return order.status === 'Pending'
}

export function needsPolling(order: OrderState): boolean {
  return order.status === 'Pending' && order.stockStatus === 'Reserving'
}

export function canConfirmOrReject(order: OrderState): boolean {
  return order.status === 'Pending' && order.stockStatus === 'Reserved'
}
