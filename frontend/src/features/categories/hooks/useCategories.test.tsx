import { renderHook, waitFor } from '@testing-library/react'
import { afterAll, afterEach, beforeAll, beforeEach, expect, it } from 'vitest'
import { resetDb } from '@/mocks/db'
import { server } from '@/mocks/server'
import { createQueryWrapper } from '@/test/queryWrapper'
import { CATEGORIES_KEY, useCategories } from './useCategories'

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
beforeEach(() => resetDb())
afterEach(() => server.resetHandlers())
afterAll(() => server.close())

it('loads categories under the shared key', async () => {
  const { client, wrapper } = createQueryWrapper()

  const { result } = renderHook(() => useCategories(), { wrapper })

  await waitFor(() => expect(result.current.isSuccess).toBe(true))
  expect(result.current.data?.map((c) => c.name)).toContain('Electronics')
  expect(client.getQueryData(CATEGORIES_KEY)).toBe(result.current.data)
})
