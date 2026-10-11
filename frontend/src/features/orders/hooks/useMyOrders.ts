import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { listMyOrders } from '../api/ordersApi'

export function useMyOrders(input: { status: string; page: number }) {
  return useQuery({
    queryKey: ['orders', 'mine', input],
    queryFn: () => listMyOrders(input),
    placeholderData: keepPreviousData,
  })
}
