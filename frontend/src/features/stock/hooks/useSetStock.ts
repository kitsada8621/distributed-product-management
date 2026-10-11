import { useMutation, useQueryClient } from '@tanstack/react-query'
import { setStock } from '../api/stockApi'

export function useSetStock() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: { productId: string; quantity: number }) => setStock(input.productId, input.quantity),
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: ['products'] }),
        queryClient.invalidateQueries({ queryKey: ['product'] }),
      ]),
  })
}
