import { useMutation, useQueryClient } from '@tanstack/react-query'
import { cancelOrder } from '../api/ordersApi'

export function useCancelOrder(orderId: string) {
  const queryClient = useQueryClient()
  const refresh = () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: ['order', orderId] }),
      queryClient.invalidateQueries({ queryKey: ['orders'] }),
    ])

  return useMutation({
    mutationFn: () => cancelOrder(orderId),
    onSuccess: refresh,
    onError: refresh,
  })
}
