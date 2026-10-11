import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterAll, afterEach, beforeAll, beforeEach, expect, it } from 'vitest'
import { resetDb } from '@/mocks/db'
import { server } from '@/mocks/server'
import { renderApp } from '@/test/renderApp'

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
beforeEach(() => resetDb())
afterEach(() => server.resetHandlers())
afterAll(() => server.close())

const SENT = 'หากอีเมลนี้มีในระบบ จะได้รับลิงก์ภายในไม่กี่นาที'

async function sendReset(email: string) {
  await userEvent.type(await screen.findByLabelText('อีเมล'), email)
  await userEvent.click(screen.getByRole('button', { name: 'ส่งลิงก์ตั้งรหัสผ่านใหม่' }))
}

it('ForgotPasswordPage shows the same message for a known and an unknown email', async () => {
  const known = renderApp('/forgot-password')
  await sendReset('user@example.com')
  expect(await screen.findByText(SENT)).toBeInTheDocument()
  known.unmount()

  renderApp('/forgot-password')
  await sendReset('nobody@example.com')
  expect(await screen.findByText(SENT)).toBeInTheDocument()
})

it('ForgotPasswordPage validates the email format', async () => {
  renderApp('/forgot-password')

  await sendReset('not-an-email')

  expect(await screen.findByText('Invalid email format')).toBeInTheDocument()
})
