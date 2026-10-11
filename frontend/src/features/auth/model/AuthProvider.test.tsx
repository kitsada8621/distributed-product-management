import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactNode } from 'react'
import { afterAll, afterEach, beforeAll, beforeEach, expect, it, vi } from 'vitest'
import { resetDb } from '@/mocks/db'
import { server } from '@/mocks/server'
import { ORDER_DRAFT_STORAGE_KEY } from '@/shared/config/storageKeys'
import { http, HttpResponse } from 'msw'
import { apiUrl } from '@/mocks/http'
import { login } from '../api/authApi'
import { AuthProvider } from './AuthProvider'
import { clearAccessToken, getAccessToken } from './session'
import { useAuth } from './useAuth'

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
beforeEach(() => resetDb())
afterEach(() => {
  server.resetHandlers()
  clearAccessToken()
  sessionStorage.clear()
  document.cookie = 'csrfToken=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/'
})
afterAll(() => server.close())

function Probe() {
  const { status, user, logout } = useAuth()
  return (
    <div>
      <span>status:{status}</span>
      <span>name:{user?.name ?? '-'}</span>
      <button onClick={logout}>logout</button>
    </div>
  )
}

function renderProbe(client = new QueryClient()) {
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={client}>
      <AuthProvider>{children}</AuthProvider>
    </QueryClientProvider>
  )
  return render(<Probe />, { wrapper })
}

it('becomes guest when there is no session cookie', async () => {
  renderProbe()

  expect(await screen.findByText('status:guest')).toBeInTheDocument()
})

it('restores the user through refresh and /me', async () => {
  await login({ email: 'user@example.com', password: 'Passw0rd!' })

  renderProbe()

  expect(await screen.findByText('status:authed')).toBeInTheDocument()
  expect(screen.getByText('name:Somchai')).toBeInTheDocument()
  expect(getAccessToken()).toMatch(/^mock\./)
})

it('becomes guest when refresh is rejected', async () => {
  await login({ email: 'user@example.com', password: 'Passw0rd!' })
  server.use(
    http.post(apiUrl('/api/auth/refresh'), () =>
      HttpResponse.json({ status: 401, code: 'REFRESH_INVALID', message: 'x', data: null }, { status: 401 }),
    ),
  )

  renderProbe()

  expect(await screen.findByText('status:guest')).toBeInTheDocument()
})

it('logout clears token, query cache and the order draft even when the API fails', async () => {
  await login({ email: 'user@example.com', password: 'Passw0rd!' })
  const client = new QueryClient()
  client.setQueryData(['x'], 1)
  sessionStorage.setItem(ORDER_DRAFT_STORAGE_KEY, '[]')
  server.use(http.post(apiUrl('/api/auth/logout'), () => HttpResponse.error()))
  const clearSpy = vi.spyOn(client, 'clear')
  renderProbe(client)
  await screen.findByText('status:authed')

  await userEvent.click(screen.getByRole('button', { name: 'logout' }))

  expect(await screen.findByText('status:guest')).toBeInTheDocument()
  expect(getAccessToken()).toBeNull()
  expect(clearSpy).toHaveBeenCalled()
  expect(sessionStorage.getItem(ORDER_DRAFT_STORAGE_KEY)).toBeNull()
})
