import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { fetchProducts } from '../api/productApi'
import type { ProductListQuery } from '../types'

export function useProducts(query: ProductListQuery) {
  return useQuery({
    queryKey: ['products', query],
    queryFn: () => fetchProducts(query),
    placeholderData: keepPreviousData,
  })
}
