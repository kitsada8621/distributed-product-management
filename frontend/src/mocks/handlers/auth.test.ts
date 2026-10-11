import { afterAll, afterEach, beforeAll, beforeEach, expect, it } from 'vitest'
import { apiRequest, setAccessTokenGetter } from '@/shared/api/httpClient'
import type { ApiError } from '@/shared/api/types'
import { resetDb } from '../db'
import { server } from '../server'

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
beforeEach(() => resetDb())
afterEach(() => {
  server.resetHandlers()
  setAccessTokenGetter(() => null)
  document.cookie = 'csrfToken=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/'
})
afterAll(() => server.close())

const login = (email = 'user@example.com', password = 'Passw0rd!') =>
  apiRequest<{ accessToken: string }>('POST', '/api/auth/login', { body: { email, password } })

const errorOf = async (promise: Promise<unknown>) => (await promise.catch((e) => e)) as ApiError

it('logs in with valid credentials', async () => {
  const result = await login()

  expect(result.accessToken).toMatch(/^mock\./)
})

it('rejects a wrong password with 401 INVALID_CREDENTIALS', async () => {
  const error = await errorOf(login('user@example.com', 'wrong'))

  expect(error.status).toBe(401)
  expect(error.code).toBe('INVALID_CREDENTIALS')
})

it('locks the account after 5 failures with 423 and lockedUntil', async () => {
  for (let i = 0; i < 5; i += 1) await errorOf(login('user@example.com', 'wrong'))

  const error = await errorOf(login())

  expect(error.status).toBe(423)
  expect(error.code).toBe('ACCOUNT_LOCKED')
  expect(error.details?.lockedUntil).toEqual(expect.any(String))
})

it('refreshes with the csrf header sent from the cookie', async () => {
  await login()

  const result = await apiRequest<{ accessToken: string }>('POST', '/api/auth/refresh')

  expect(result.accessToken).toMatch(/^mock\./)
})

it('refuses refresh without the csrf header with 403 CSRF_INVALID', async () => {
  await login()
  document.cookie = 'csrfToken=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/'

  const error = await errorOf(apiRequest('POST', '/api/auth/refresh'))

  expect(error.status).toBe(403)
  expect(error.code).toBe('CSRF_INVALID')
})

it('refuses refresh after logout', async () => {
  const { accessToken } = await login()
  setAccessTokenGetter(() => accessToken)
  const csrf = document.cookie
  await apiRequest('POST', '/api/auth/logout')
  document.cookie = csrf

  const error = await errorOf(apiRequest('POST', '/api/auth/refresh'))

  expect(error.status).toBe(403)
})

it('returns the current user from /me with a Bearer token', async () => {
  const { accessToken } = await login()
  setAccessTokenGetter(() => accessToken)

  const me = await apiRequest('GET', '/api/auth/me')

  expect(me).toEqual({ id: 'u-user', email: 'user@example.com', name: 'Somchai', role: 'user' })
})

it('returns 401 from /me without a Bearer token', async () => {
  const error = await errorOf(apiRequest('GET', '/api/auth/me'))

  expect(error.status).toBe(401)
})

it('registers a new user and rejects a duplicate email', async () => {
  const body = { email: 'new@example.com', password: 'Passw0rd!', name: 'New' }
  await apiRequest('POST', '/api/auth/register', { body })

  const error = await errorOf(apiRequest('POST', '/api/auth/register', { body }))

  expect(error.status).toBe(409)
  expect(error.code).toBe('EMAIL_EXISTS')
})
