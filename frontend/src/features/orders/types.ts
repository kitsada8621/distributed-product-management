export type OrderStatus = 'Pending' | 'Confirmed' | 'Rejected' | 'Cancelled'
export type StockStatus = 'Reserving' | 'Reserved'

export type PaymentMethod = 'CASH_ON_DELIVERY' | 'PAY_AT_STORE'

export interface ShippingAddress {
  recipientName: string
  phone: string
  addressLine: string
  province: string
  postalCode: string
}

export interface OrderItem {
  productId: string
  name: string | null
  unitPrice: number | null
  quantity: number
}

export interface RejectionFailedItem {
  productId: string
  reason: 'INSUFFICIENT_STOCK' | 'PRODUCT_NOT_FOUND'
  requested: number
  available: number
}

export interface Rejection {
  type: 'STOCK_UNAVAILABLE' | 'TIMEOUT' | 'ADMIN_REJECTED'
  message: string | null
  failedItems: RejectionFailedItem[]
}

export interface Order {
  id: string
  status: OrderStatus
  stockStatus: StockStatus | null
  items: OrderItem[]
  total: number | null
  shippingAddress: ShippingAddress
  paymentMethod: PaymentMethod
  rejection: Rejection | null
  ownerEmail?: string
  createdAt: string
  updatedAt: string
}

export interface OrderRow {
  id: string
  status: OrderStatus
  stockStatus: StockStatus | null
  total: number | null
  createdAt: string
  ownerEmail?: string
}

export interface CreateOrderInput {
  items: { productId: string; quantity: number }[]
  shippingAddress: ShippingAddress
  paymentMethod: PaymentMethod
}

export interface CreateOrderResult {
  orderId: string
  status: OrderStatus
  stockStatus: StockStatus | null
}
