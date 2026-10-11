import { useMutation, useQueryClient } from '@tanstack/react-query'
import { confirmOrder, rejectOrder } from '../api/adminOrdersApi'

function useRefreshOrders(orderId: string) {
  const queryClient = useQueryClient()
  return () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: ['order', 'admin', orderId] }),
      queryClient.invalidateQueries({ queryKey: ['orders'] }),
    ])
}

export function useConfirmOrder(orderId: string) {
  const refresh = useRefreshOrders(orderId)
  return useMutation({ mutationFn: () => confirmOrder(orderId), onSuccess: refresh, onError: refresh })
}

export function useRejectOrder(orderId: string) {
  const refresh = useRefreshOrders(orderId)
  return useMutation({
    mutationFn: (reason: string) => rejectOrder(orderId, reason),
    onSuccess: refresh,
    onError: refresh,
  })
}
