import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { listAllOrders } from '../api/adminOrdersApi'

export function useAdminOrders(input: { status: string; page: number }) {
  return useQuery({
    queryKey: ['orders', 'admin', input],
    queryFn: () => listAllOrders(input),
    placeholderData: keepPreviousData,
  })
}
