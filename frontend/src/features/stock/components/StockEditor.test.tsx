import { render, renderHook, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterAll, afterEach, beforeAll, beforeEach, expect, it } from 'vitest'
import { db } from '@/mocks/db'
import { server } from '@/mocks/server'
import type { ApiError } from '@/shared/api/types'
import { createQueryWrapper } from '@/test/queryWrapper'
import { resetTestState, signIn } from '@/test/session'
import { useSetStock } from '../hooks/useSetStock'
import { StockEditor } from './StockEditor'

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
beforeEach(async () => {
  resetTestState()
  await signIn('admin')
})
afterEach(() => server.resetHandlers())
afterAll(() => server.close())

it('reports typed values and shows an error', async () => {
  let latest = ''
  const { rerender } = render(<StockEditor value="" onChange={(v) => (latest = v)} />)

  await userEvent.type(screen.getByLabelText('จำนวนสต็อกใหม่ (แทนที่ค่าปัจจุบัน)'), '7')
  rerender(<StockEditor value="7" onChange={() => {}} error="Stock must be a whole number of 0 or more" />)

  expect(latest).toBe('7')
  expect(screen.getByText('Stock must be a whole number of 0 or more')).toBeInTheDocument()
})

it('sets the absolute stock value', async () => {
  const { wrapper } = createQueryWrapper()
  const { result } = renderHook(() => useSetStock(), { wrapper })

  result.current.mutate({ productId: 'p-2', quantity: 40 })

  await waitFor(() => expect(result.current.isSuccess).toBe(true))
  expect(db.products.find((p) => p.id === 'p-2')?.stock).toBe(40)
})

it('rejects a negative value with STOCK_NEGATIVE', async () => {
  const { wrapper } = createQueryWrapper()
  const { result } = renderHook(() => useSetStock(), { wrapper })

  result.current.mutate({ productId: 'p-2', quantity: -1 })

  await waitFor(() => expect(result.current.isError).toBe(true))
  expect((result.current.error as ApiError).code).toBe('STOCK_NEGATIVE')
})

it('returns 404 for a product that no longer exists', async () => {
  const { wrapper } = createQueryWrapper()
  const { result } = renderHook(() => useSetStock(), { wrapper })

  result.current.mutate({ productId: 'gone', quantity: 3 })

  await waitFor(() => expect(result.current.isError).toBe(true))
  expect((result.current.error as ApiError).status).toBe(404)
})
