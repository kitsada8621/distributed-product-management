import { afterAll, afterEach, beforeAll, beforeEach, expect, it } from 'vitest'
import { resetDb } from '@/mocks/db'
import { server } from '@/mocks/server'
import { clearAccessToken, setAccessToken } from '../model/session'
import * as authApi from './authApi'

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
beforeEach(() => resetDb())
afterEach(() => {
  server.resetHandlers()
  clearAccessToken()
  document.cookie = 'csrfToken=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/'
})
afterAll(() => server.close())

it('logs in, refreshes without a body and reads the current user', async () => {
  const { accessToken } = await authApi.login({ email: 'user@example.com', password: 'Passw0rd!' })
  const refreshed = await authApi.refresh()
  setAccessToken(refreshed.accessToken)

  expect(accessToken).toMatch(/^mock\./)
  await expect(authApi.fetchMe()).resolves.toEqual({
    id: 'u-user',
    email: 'user@example.com',
    name: 'Somchai',
    role: 'user',
  })
})

it('logs out', async () => {
  const { accessToken } = await authApi.login({ email: 'user@example.com', password: 'Passw0rd!' })
  setAccessToken(accessToken)

  await expect(authApi.logout()).resolves.toBeNull()
})

it('registers a user', async () => {
  const created = await authApi.register({ email: 'n@example.com', password: 'Passw0rd!', name: 'N' })

  expect(created.role).toBe('user')
})
