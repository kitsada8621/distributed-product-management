import { OrderTable, type OrderRow } from '@/features/orders'
import { PATHS } from '@/shared/config/paths'

export function AdminOrderTable({ orders }: { orders: OrderRow[] }) {
  return <OrderTable orders={orders} detailPath={PATHS.adminOrderDetail} showOwner />
}
