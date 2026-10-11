import { renderHook, waitFor } from '@testing-library/react'
import { afterAll, afterEach, beforeAll, beforeEach, expect, it } from 'vitest'
import { resetDb } from '@/mocks/db'
import { server } from '@/mocks/server'
import { createQueryWrapper } from '@/test/queryWrapper'
import { useProducts } from './useProducts'

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
beforeEach(() => resetDb())
afterEach(() => server.resetHandlers())
afterAll(() => server.close())

it('returns a page of products', async () => {
  const { wrapper } = createQueryWrapper()

  const { result } = renderHook(() => useProducts({ q: '', categoryId: '', page: 1 }), { wrapper })

  await waitFor(() => expect(result.current.isSuccess).toBe(true))
  expect(result.current.data).toMatchObject({ page: 1, pageSize: 20, total: 24 })
  expect(result.current.data?.items).toHaveLength(20)
})

it('passes q and categoryId to the server', async () => {
  const { wrapper } = createQueryWrapper()

  const { result } = renderHook(() => useProducts({ q: 'mouse', categoryId: 'c-accessories', page: 1 }), { wrapper })

  await waitFor(() => expect(result.current.isSuccess).toBe(true))
  expect(result.current.data?.items.map((p) => p.name)).toEqual(['Mouse Pad'])
})
