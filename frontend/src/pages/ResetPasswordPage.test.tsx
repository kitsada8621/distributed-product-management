import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterAll, afterEach, beforeAll, beforeEach, expect, it } from 'vitest'
import { db, resetDb } from '@/mocks/db'
import { server } from '@/mocks/server'
import { renderApp } from '@/test/renderApp'

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
beforeEach(() => resetDb())
afterEach(() => server.resetHandlers())
afterAll(() => server.close())

it('ResetPasswordPage shows an error right away when there is no token', async () => {
  renderApp('/reset-password')

  expect(await screen.findByText(/ลิงก์ไม่ถูกต้องหรือหมดอายุ/)).toBeInTheDocument()
  expect(screen.getByRole('link', { name: 'ขอลิงก์ใหม่' })).toBeInTheDocument()
})

it('ResetPasswordPage removes the token from the URL', async () => {
  window.history.pushState(null, '', '/reset-password?token=abc')
  renderApp('/reset-password?token=abc')

  await screen.findByLabelText('รหัสผ่านใหม่')

  await waitFor(() => expect(window.location.search).toBe(''))
})

it('ResetPasswordPage explains an invalid token with a link to request a new one', async () => {
  renderApp('/reset-password?token=wrong')

  await userEvent.type(await screen.findByLabelText('รหัสผ่านใหม่'), 'NewPassw0rd!')
  await userEvent.type(screen.getByLabelText('ยืนยันรหัสผ่านใหม่'), 'NewPassw0rd!')
  await userEvent.click(screen.getByRole('button', { name: 'บันทึกรหัสผ่านใหม่' }))

  expect(await screen.findByText(/ลิงก์ไม่ถูกต้องหรือหมดอายุ/)).toBeInTheDocument()
  expect(screen.getByRole('link', { name: 'ขอลิงก์ใหม่' })).toBeInTheDocument()
})

it('ResetPasswordPage changes the password and goes to /login', async () => {
  db.resetTokens.set('good', 'user@example.com')
  const { router } = renderApp('/reset-password?token=good')

  await userEvent.type(await screen.findByLabelText('รหัสผ่านใหม่'), 'NewPassw0rd!')
  await userEvent.type(screen.getByLabelText('ยืนยันรหัสผ่านใหม่'), 'NewPassw0rd!')
  await userEvent.click(screen.getByRole('button', { name: 'บันทึกรหัสผ่านใหม่' }))

  await waitFor(() => expect(router.state.location.pathname).toBe('/login'))
  expect(db.users.find((u) => u.email === 'user@example.com')?.password).toBe('NewPassw0rd!')
})

it('ResetPasswordPage blocks a short password and stays on the page', async () => {
  const { router } = renderApp('/reset-password?token=good')

  await userEvent.type(await screen.findByLabelText('รหัสผ่านใหม่'), 'Zk7!mQp')
  await userEvent.type(screen.getByLabelText('ยืนยันรหัสผ่านใหม่'), 'Zk7!mQp')
  await userEvent.click(screen.getByRole('button', { name: 'บันทึกรหัสผ่านใหม่' }))

  expect(await screen.findByText('รหัสผ่านต้องมีอย่างน้อย 8 ตัวอักษร')).toBeInTheDocument()
  expect(router.state.location.pathname).toBe('/reset-password')
})

it('ResetPasswordPage toggles the new password field', async () => {
  renderApp('/reset-password?token=abc')
  const input = await screen.findByLabelText('รหัสผ่านใหม่')
  expect(input).toHaveAttribute('type', 'password')

  await userEvent.click(screen.getAllByRole('button', { name: 'แสดงรหัสผ่าน' })[0])

  expect(input).toHaveAttribute('type', 'text')
})
