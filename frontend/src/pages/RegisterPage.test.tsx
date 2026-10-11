import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { afterAll, afterEach, beforeAll, beforeEach, expect, it } from 'vitest'
import { resetDb } from '@/mocks/db'
import { apiUrl } from '@/mocks/http'
import { server } from '@/mocks/server'
import { renderApp } from '@/test/renderApp'

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
beforeEach(() => resetDb())
afterEach(() => server.resetHandlers())
afterAll(() => server.close())

async function fill(values: { email?: string; name?: string; password?: string; confirm?: string }) {
  const email = await screen.findByLabelText('อีเมล')
  if (values.email) await userEvent.type(email, values.email)
  if (values.name) await userEvent.type(screen.getByLabelText('ชื่อ'), values.name)
  if (values.password) await userEvent.type(screen.getByLabelText('รหัสผ่าน'), values.password)
  if (values.confirm) await userEvent.type(screen.getByLabelText('ยืนยันรหัสผ่าน'), values.confirm)
  await userEvent.click(screen.getByRole('button', { name: 'สมัครสมาชิก' }))
}

it('has no role field', async () => {
  renderApp('/register')

  await screen.findByLabelText('อีเมล')

  expect(screen.queryByLabelText(/role/i)).not.toBeInTheDocument()
})

it('validates required fields and password rules on the client', async () => {
  renderApp('/register')

  await fill({ email: 'bad', name: ' ', password: 'short', confirm: 'different' })

  expect(await screen.findByText('Invalid email format')).toBeInTheDocument()
  expect(screen.getByText('Name is required')).toBeInTheDocument()
  expect(screen.getByText('รหัสผ่านต้องมีอย่างน้อย 8 ตัวอักษร')).toBeInTheDocument()
  expect(screen.getByText('Passwords do not match')).toBeInTheDocument()
})

it('shows EMAIL_EXISTS on the email field', async () => {
  renderApp('/register')

  await fill({ email: 'user@example.com', name: 'X', password: 'Orange-Bicycle-77', confirm: 'Orange-Bicycle-77' })

  expect(await screen.findByText('อีเมลนี้ถูกใช้สมัครแล้ว')).toBeInTheDocument()
})

it('shows server 422 field errors', async () => {
  server.use(
    http.post(apiUrl('/api/auth/register'), () =>
      HttpResponse.json(
        {
          status: 422,
          code: 'VALIDATION_ERROR',
          message: 'Invalid input',
          data: null,
          errors: [{ field: 'name', code: 'REQUIRED', message: 'Name is rejected by the server' }],
        },
        { status: 422 },
      ),
    ),
  )
  renderApp('/register')

  await fill({ email: 'new@example.com', name: 'X', password: 'Orange-Bicycle-77', confirm: 'Orange-Bicycle-77' })

  expect(await screen.findByText('Name is rejected by the server')).toBeInTheDocument()
})

it('goes to /login with a success message and does not log in', async () => {
  const { router } = renderApp('/register')

  await fill({ email: 'new@example.com', name: 'New', password: 'Orange-Bicycle-77', confirm: 'Orange-Bicycle-77' })

  await waitFor(() => expect(router.state.location.pathname).toBe('/login'))
  expect(await screen.findByText('สร้างบัญชีแล้ว เข้าสู่ระบบได้เลย')).toBeInTheDocument()
})

it('blocks a password that contains the name or email and stays on /register', async () => {
  const { router } = renderApp('/register')

  await fill({ email: 'new@example.com', name: 'Somchai', password: 'My-somchai-9!', confirm: 'My-somchai-9!' })

  expect(await screen.findByText('รหัสผ่านต้องไม่มีอีเมลหรือชื่อของคุณ')).toBeInTheDocument()
  expect(router.state.location.pathname).toBe('/register')
})

it('gives both password fields their own show/hide toggle', async () => {
  renderApp('/register')
  const password = await screen.findByLabelText('รหัสผ่าน')
  const confirm = screen.getByLabelText('ยืนยันรหัสผ่าน')

  await userEvent.click(screen.getAllByRole('button', { name: 'แสดงรหัสผ่าน' })[0])

  expect(password).toHaveAttribute('type', 'text')
  expect(confirm).toHaveAttribute('type', 'password')
  expect(screen.getAllByRole('button', { name: /รหัสผ่าน/ })).toHaveLength(2)
})
