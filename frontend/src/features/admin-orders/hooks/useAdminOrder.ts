import { useQuery } from '@tanstack/react-query'
import { needsPolling } from '@/features/orders'
import { POLL_INTERVAL_MS } from '@/shared/config/env'
import { getAdminOrder } from '../api/adminOrdersApi'

export function useAdminOrder(id: string) {
  return useQuery({
    queryKey: ['order', 'admin', id],
    queryFn: () => getAdminOrder(id),
    refetchInterval: (current) => {
      const order = current.state.data
      return order && needsPolling(order) ? POLL_INTERVAL_MS : false
    },
  })
}
