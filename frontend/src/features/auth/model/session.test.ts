import { afterEach, expect, it, vi } from 'vitest'
import { apiRequest } from '@/shared/api/httpClient'
import { clearAccessToken, getAccessToken, setAccessToken } from './session'

afterEach(() => {
  clearAccessToken()
  vi.restoreAllMocks()
})

it('keeps the token in memory and clears it', () => {
  setAccessToken('abc')
  expect(getAccessToken()).toBe('abc')

  clearAccessToken()
  expect(getAccessToken()).toBeNull()
})

it('never writes to localStorage or sessionStorage', () => {
  const setItem = vi.spyOn(Storage.prototype, 'setItem')

  setAccessToken('abc')
  clearAccessToken()

  expect(setItem).not.toHaveBeenCalled()
})

it('feeds the http client with the in-memory token', async () => {
  const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response('{}', { status: 200 }))
  setAccessToken('abc')

  await apiRequest('GET', '/api/auth/me')

  const headers = fetchSpy.mock.calls[0][1]?.headers as Headers
  expect(headers.get('Authorization')).toBe('Bearer abc')
})
