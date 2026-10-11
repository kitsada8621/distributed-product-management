import { act, renderHook } from '@testing-library/react'
import { afterEach, beforeEach, expect, it, vi } from 'vitest'
import { useDebounce } from './useDebounce'

beforeEach(() => vi.useFakeTimers())
afterEach(() => vi.useRealTimers())

it('updates the value only after the delay', () => {
  const { result, rerender } = renderHook(({ value }) => useDebounce(value, 300), {
    initialProps: { value: 'a' },
  })

  rerender({ value: 'ab' })
  act(() => vi.advanceTimersByTime(299))
  expect(result.current).toBe('a')

  act(() => vi.advanceTimersByTime(1))
  expect(result.current).toBe('ab')
})
