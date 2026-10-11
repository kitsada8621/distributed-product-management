import { useMutation, useQueryClient } from '@tanstack/react-query'
import { createCategory, deleteCategory, updateCategory } from '../api/categoryApi'
import { CATEGORIES_KEY } from './useCategories'

function useRefreshCategories() {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries({ queryKey: CATEGORIES_KEY })
}

export function useCreateCategory() {
  const refresh = useRefreshCategories()
  return useMutation({ mutationFn: (name: string) => createCategory(name), onSuccess: refresh })
}

export function useUpdateCategory() {
  const refresh = useRefreshCategories()
  return useMutation({
    mutationFn: (input: { id: string; name: string }) => updateCategory(input.id, input.name),
    onSuccess: refresh,
  })
}

export function useDeleteCategory() {
  const refresh = useRefreshCategories()
  return useMutation({ mutationFn: (id: string) => deleteCategory(id), onSuccess: refresh })
}
