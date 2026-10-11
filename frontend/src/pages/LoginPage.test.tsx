import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { afterAll, afterEach, beforeAll, beforeEach, expect, it } from 'vitest'
import { db, resetDb } from '@/mocks/db'
import { apiUrl } from '@/mocks/http'
import { server } from '@/mocks/server'
import { clearAccessToken } from '@/features/auth'
import { renderApp } from '@/test/renderApp'

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
beforeEach(() => resetDb())
afterEach(() => {
  server.resetHandlers()
  clearAccessToken()
  sessionStorage.clear()
  document.cookie = 'csrfToken=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/'
})
afterAll(() => server.close())

async function fillAndSubmit(email: string, password: string) {
  await userEvent.type(await screen.findByLabelText('อีเมล'), email)
  await userEvent.type(screen.getByLabelText('รหัสผ่าน'), password)
  await userEvent.click(screen.getByRole('button', { name: 'เข้าสู่ระบบ' }))
}

it('shows validation messages for empty fields', async () => {
  renderApp('/login')

  await userEvent.click(await screen.findByRole('button', { name: 'เข้าสู่ระบบ' }))

  expect(await screen.findByText('Invalid email format')).toBeInTheDocument()
  expect(screen.getByText('Password is required')).toBeInTheDocument()
})

it('shows a neutral message for wrong credentials', async () => {
  renderApp('/login')

  await fillAndSubmit('user@example.com', 'wrong-password')

  expect(await screen.findByText('อีเมลหรือรหัสผ่านไม่ถูกต้อง')).toBeInTheDocument()
})

it('shows the unlock countdown for a locked account', async () => {
  db.loginFailures.set('user@example.com', {
    count: 5,
    lockedUntil: new Date(Date.now() + 10 * 60_000).toISOString(),
  })
  renderApp('/login')

  await fillAndSubmit('user@example.com', 'Passw0rd!')

  expect(await screen.findByText(/ลองใหม่ในอีก (9:5\d|10:00)/)).toBeInTheDocument()
})

it('shows a retry-later message on 429', async () => {
  server.use(
    http.post(apiUrl('/api/auth/login'), () =>
      HttpResponse.json({ status: 429, code: 'RATE_LIMITED', message: 'slow down', data: null }, { status: 429 }),
    ),
  )
  renderApp('/login')

  await fillAndSubmit('user@example.com', 'Passw0rd!')

  expect(await screen.findByText('พยายามหลายครั้งเกินไป กรุณารอสักครู่แล้วลองใหม่')).toBeInTheDocument()
})

it('shows the service banner when the server fails', async () => {
  server.use(http.post(apiUrl('/api/auth/login'), () => HttpResponse.error()))
  renderApp('/login')

  await fillAndSubmit('user@example.com', 'Passw0rd!')

  expect(await screen.findByRole('alert')).toHaveTextContent('ระบบไม่พร้อมให้บริการชั่วคราว')
})

it('shows the session expired alert when flagged', async () => {
  renderApp('/login?expired=1')

  expect(await screen.findByText('เซสชันหมดอายุ กรุณาเข้าสู่ระบบอีกครั้ง')).toBeInTheDocument()
})

it('redirects to returnTo after a successful login', async () => {
  const { router } = renderApp('/login?returnTo=%2Forders')

  await fillAndSubmit('user@example.com', 'Passw0rd!')

  await waitFor(() => expect(router.state.location.pathname).toBe('/orders'))
})

it('goes to /products when there is no returnTo', async () => {
  const { router } = renderApp('/login')

  await fillAndSubmit('user@example.com', 'Passw0rd!')

  await waitFor(() => expect(router.state.location.pathname).toBe('/products'))
})

it('toggles the password field visibility', async () => {
  renderApp('/login')
  const password = await screen.findByLabelText('รหัสผ่าน')
  expect(password).toHaveAttribute('type', 'password')

  await userEvent.click(screen.getByRole('button', { name: 'แสดงรหัสผ่าน' }))

  expect(password).toHaveAttribute('type', 'text')
})
