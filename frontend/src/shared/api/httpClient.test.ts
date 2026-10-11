// @vitest-environment jsdom
import { http, HttpResponse, delay } from 'msw'
import { setupServer } from 'msw/node'
import { afterAll, afterEach, beforeAll, expect, it } from 'vitest'
import { API_BASE_URL } from '@/shared/config/env'
import { apiRequest, apiRequestPage, setAccessTokenGetter } from './httpClient'
import type { ApiEnvelope, ApiError } from './types'

const server = setupServer()
const url = (path: string) => `${API_BASE_URL}${path}`

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
afterEach(() => {
  server.resetHandlers()
  setAccessTokenGetter(() => null)
  document.cookie = 'csrfToken=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/'
})
afterAll(() => server.close())

function ok(data: unknown, meta: unknown = null) {
  return HttpResponse.json({ status: 200, success: true, code: 'SUCCESS', message: '', data, meta, errors: null })
}

it('accepts an envelope without correlationId and timestamp, like Catalog and Order send', async () => {
  const body: ApiEnvelope<{ id: string }[]> = {
    status: 200,
    success: true,
    code: 'SUCCESS',
    message: 'OK',
    data: [{ id: 'c1' }],
    meta: null,
    errors: null,
  }
  server.use(http.get(url('/api/catalog/categories'), () => HttpResponse.json(body)))

  const data = await apiRequest('GET', '/api/catalog/categories')

  expect(data).toEqual([{ id: 'c1' }])
})

it('attaches the Bearer token when one exists', async () => {
  let seen: string | null = null
  server.use(
    http.get(url('/api/catalog/categories'), ({ request }) => {
      seen = request.headers.get('Authorization')
      return ok([])
    }),
  )
  setAccessTokenGetter(() => 'abc')

  await apiRequest('GET', '/api/catalog/categories')

  expect(seen).toBe('Bearer abc')
})

it('sends X-CSRF-Token only on refresh and logout', async () => {
  const seen: Record<string, string | null> = {}
  server.use(
    http.post(url('/api/auth/refresh'), ({ request }) => {
      seen.refresh = request.headers.get('X-CSRF-Token')
      return ok({ accessToken: 'x' })
    }),
    http.post(url('/api/auth/login'), ({ request }) => {
      seen.login = request.headers.get('X-CSRF-Token')
      return ok({ accessToken: 'x' })
    }),
  )
  document.cookie = 'csrfToken=token-1; path=/'

  await apiRequest('POST', '/api/auth/refresh')
  await apiRequest('POST', '/api/auth/login', { body: { email: 'a', password: 'b' } })

  expect(seen.refresh).toBe('token-1')
  expect(seen.login).toBeNull()
})

it('returns undefined data for an empty 204 body', async () => {
  server.use(http.delete(url('/api/catalog/products/1'), () => new HttpResponse(null, { status: 204 })))

  await expect(apiRequest('DELETE', '/api/catalog/products/1')).resolves.toBeUndefined()
})

it('turns the envelope meta into a Page', async () => {
  server.use(
    http.get(url('/api/catalog/products'), ({ request }) => {
      const q = new URL(request.url).searchParams.get('q')
      return ok([{ id: q }], { page: 2, pageSize: 20, total: 41, totalPages: 3 })
    }),
  )

  const page = await apiRequestPage('/api/catalog/products', { query: { q: 'kb', categoryId: undefined } })

  expect(page).toEqual({ items: [{ id: 'kb' }], page: 2, pageSize: 20, total: 41 })
})

it('maps an error response including the correlation id header', async () => {
  server.use(
    http.get(url('/api/orders/9'), () =>
      HttpResponse.json(
        { status: 404, success: false, code: 'NOT_FOUND', message: 'Not found', data: null, errors: null },
        { status: 404, headers: { 'X-Correlation-Id': 'cid-9' } },
      ),
    ),
  )

  const error = (await apiRequest('GET', '/api/orders/9').catch((e) => e)) as ApiError

  expect(error.status).toBe(404)
  expect(error.code).toBe('NOT_FOUND')
  expect(error.correlationId).toBe('cid-9')
})

it('maps a timeout to NETWORK_ERROR', async () => {
  server.use(
    http.get(url('/api/slow'), async () => {
      await delay(200)
      return ok(null)
    }),
  )

  const error = (await apiRequest('GET', '/api/slow', { timeoutMs: 20 }).catch((e) => e)) as ApiError

  expect(error.code).toBe('NETWORK_ERROR')
  expect(error.status).toBe(0)
})
