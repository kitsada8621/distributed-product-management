import { useMutation, useQueryClient } from '@tanstack/react-query'
import { createOrder } from '../api/ordersApi'
import type { CreateOrderInput } from '../types'

export function useCreateOrder() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: { order: CreateOrderInput; idempotencyKey: string }) =>
      createOrder(input.order, input.idempotencyKey),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['orders'] }),
  })
}
