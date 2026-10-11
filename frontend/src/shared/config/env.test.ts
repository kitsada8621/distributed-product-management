import { expect, it } from 'vitest'
import {
  PAGE_SIZE,
  POLL_INTERVAL_MS,
  POLL_TIMEOUT_MS,
  REQUEST_TIMEOUT_MS,
  SEARCH_DEBOUNCE_MS,
} from './env'

it('exposes the documented defaults', () => {
  expect(POLL_INTERVAL_MS).toBe(2000)
  expect(POLL_TIMEOUT_MS).toBe(70000)
  expect(REQUEST_TIMEOUT_MS).toBe(15000)
  expect(SEARCH_DEBOUNCE_MS).toBe(300)
  expect(PAGE_SIZE).toBe(20)
})
