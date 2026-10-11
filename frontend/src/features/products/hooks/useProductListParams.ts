import { useEffect, useState } from 'react'
import { useDebounce, usePagination } from '@/shared/hooks'

export function useProductListParams() {
  const { params, setParams } = usePagination()
  const [search, setSearch] = useState(params.q)
  const debouncedSearch = useDebounce(search)

  useEffect(() => {
    if (debouncedSearch !== params.q) setParams({ q: debouncedSearch })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedSearch])

  return { params, setParams, search, setSearch }
}
