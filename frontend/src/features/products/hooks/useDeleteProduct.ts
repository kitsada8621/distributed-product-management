import { useMutation, useQueryClient } from '@tanstack/react-query'
import { deleteProduct } from '../api/productApi'

export function useDeleteProduct() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deleteProduct(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['products'] }),
  })
}
