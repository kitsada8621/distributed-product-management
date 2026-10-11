import { act, renderHook } from '@testing-library/react'
import type { ReactNode } from 'react'
import { MemoryRouter } from 'react-router'
import { expect, it } from 'vitest'
import { usePagination } from './usePagination'

function wrapperAt(url: string) {
  return ({ children }: { children: ReactNode }) => <MemoryRouter initialEntries={[url]}>{children}</MemoryRouter>
}

it('reads page and filters from the URL', () => {
  const { result } = renderHook(() => usePagination(), { wrapper: wrapperAt('/?page=3&q=kb&categoryId=c1') })

  expect(result.current.params).toEqual({ page: 3, q: 'kb', categoryId: 'c1', status: '' })
})

it('falls back to page 1 for an invalid page', () => {
  const { result } = renderHook(() => usePagination(), { wrapper: wrapperAt('/?page=abc') })

  expect(result.current.params.page).toBe(1)
})

it('resets page to 1 when the search text changes', () => {
  const { result } = renderHook(() => usePagination(), { wrapper: wrapperAt('/?page=3') })

  act(() => result.current.setParams({ q: 'mouse' }))

  expect(result.current.params).toMatchObject({ page: 1, q: 'mouse' })
})

it('keeps the filters when only the page changes', () => {
  const { result } = renderHook(() => usePagination(), { wrapper: wrapperAt('/?q=kb') })

  act(() => result.current.setParams({ page: 2 }))

  expect(result.current.params).toMatchObject({ page: 2, q: 'kb' })
})
