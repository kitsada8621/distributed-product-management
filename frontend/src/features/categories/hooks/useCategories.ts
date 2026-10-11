import { useQuery } from '@tanstack/react-query'
import { fetchCategories } from '../api/categoryApi'

const FIVE_MINUTES_MS = 5 * 60 * 1000

export const CATEGORIES_KEY = ['categories']

export function useCategories() {
  return useQuery({
    queryKey: CATEGORIES_KEY,
    queryFn: fetchCategories,
    staleTime: FIVE_MINUTES_MS,
  })
}
