import { useCallback } from 'react'
import { useSearchParams } from 'react-router'

export interface ListParams {
  page: number
  q: string
  categoryId: string
  status: string
}

type ListParamsPatch = Partial<ListParams>

function toPage(raw: string | null): number {
  const page = Number(raw)
  return Number.isInteger(page) && page >= 1 ? page : 1
}

export function usePagination() {
  const [searchParams, setSearchParams] = useSearchParams()

  const params: ListParams = {
    page: toPage(searchParams.get('page')),
    q: searchParams.get('q') ?? '',
    categoryId: searchParams.get('categoryId') ?? '',
    status: searchParams.get('status') ?? '',
  }

  const setParams = useCallback(
    (patch: ListParamsPatch) => {
      const next = new URLSearchParams(searchParams)
      const changesFilter = patch.q !== undefined || patch.categoryId !== undefined || patch.status !== undefined

      for (const [key, value] of Object.entries(patch)) {
        if (value === '' || value === undefined) next.delete(key)
        else next.set(key, String(value))
      }
      if (changesFilter && patch.page === undefined) next.delete('page')

      setSearchParams(next)
    },
    [searchParams, setSearchParams],
  )

  return { params, setParams }
}
