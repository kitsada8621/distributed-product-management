import { useMutation, useQueryClient } from '@tanstack/react-query'
import { createProduct, updateProduct, type ProductPayload } from '../api/productApi'

function useRefreshProducts() {
  const queryClient = useQueryClient()
  return () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: ['products'] }),
      queryClient.invalidateQueries({ queryKey: ['product'] }),
    ])
}

export function useCreateProduct() {
  const refresh = useRefreshProducts()
  return useMutation({
    mutationFn: (input: ProductPayload & { stock: number }) => createProduct(input),
    onSuccess: refresh,
  })
}

export function useUpdateProduct() {
  const refresh = useRefreshProducts()
  return useMutation({
    mutationFn: (input: { id: string; payload: ProductPayload }) => updateProduct(input.id, input.payload),
    onSuccess: refresh,
  })
}
