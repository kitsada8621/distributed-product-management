import { http, HttpResponse } from 'msw'
import { afterAll, afterEach, beforeAll, beforeEach, expect, it, vi } from 'vitest'
import { expireAccessTokens, resetDb } from '@/mocks/db'
import { apiUrl } from '@/mocks/http'
import { server } from '@/mocks/server'
import { apiRequest, setAuthRefreshHandlers } from '@/shared/api/httpClient'
import type { ApiError } from '@/shared/api/types'
import { login } from '../api/authApi'
import { refreshAccessToken } from './refreshSession'
import { clearAccessToken, setAccessToken } from './session'

const onExpired = vi.fn()
let refreshCalls = 0

beforeAll(() => {
  server.listen({ onUnhandledRequest: 'error' })
  server.events.on('request:start', ({ request }) => {
    if (request.url.endsWith('/api/auth/refresh')) refreshCalls += 1
  })
})
beforeEach(async () => {
  resetDb()
  refreshCalls = 0
  onExpired.mockClear()
  setAuthRefreshHandlers({ refresh: refreshAccessToken, onExpired })
  const { accessToken } = await login({ email: 'user@example.com', password: 'Passw0rd!' })
  setAccessToken(accessToken)
})
afterEach(() => {
  server.resetHandlers()
  setAuthRefreshHandlers(null)
  clearAccessToken()
  document.cookie = 'csrfToken=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/'
})
afterAll(() => server.close())

const errorOf = async (promise: Promise<unknown>) => (await promise.catch((e) => e)) as ApiError

it('refreshes once on 401 and retries the original request', async () => {
  expireAccessTokens()

  const me = await apiRequest<{ name: string }>('GET', '/api/auth/me')

  expect(me.name).toBe('Somchai')
  expect(refreshCalls).toBe(1)
})

it('shares one refresh between parallel 401s', async () => {
  expireAccessTokens()

  await Promise.all([apiRequest('GET', '/api/auth/me'), apiRequest('GET', '/api/auth/me')])

  expect(refreshCalls).toBe(1)
})

it('retries only once when the retry is also 401', async () => {
  server.use(http.get(apiUrl('/api/auth/me'), () => HttpResponse.json({ code: 'UNAUTHORIZED' }, { status: 401 })))

  const error = await errorOf(apiRequest('GET', '/api/auth/me'))

  expect(error.status).toBe(401)
  expect(refreshCalls).toBe(1)
})

it('reports the session as expired when refresh fails and keeps the original 401', async () => {
  expireAccessTokens()
  server.use(http.post(apiUrl('/api/auth/refresh'), () => HttpResponse.json({ code: 'REFRESH_INVALID' }, { status: 401 })))

  const error = await errorOf(apiRequest('GET', '/api/auth/me'))

  expect(error.code).toBe('UNAUTHORIZED')
  expect(onExpired).toHaveBeenCalledOnce()
})

it('does not refresh when /login returns 401', async () => {
  const error = await errorOf(
    apiRequest('POST', '/api/auth/login', { body: { email: 'user@example.com', password: 'wrong' } }),
  )

  expect(error.code).toBe('INVALID_CREDENTIALS')
  expect(refreshCalls).toBe(0)
})
