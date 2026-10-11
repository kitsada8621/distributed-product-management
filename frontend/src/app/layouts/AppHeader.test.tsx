import { screen } from '@testing-library/react'
import { expect, it } from 'vitest'
import { AuthContext, type AuthContextValue } from '@/features/auth'
import { renderWithProviders } from '@/test/testUtils'
import { AppHeader } from './AppHeader'

function renderHeader(value: Partial<AuthContextValue>, route = '/') {
  const auth: AuthContextValue = {
    status: 'guest',
    user: null,
    sessionExpired: false,
    login: async () => {},
    logout: async () => {},
    ...value,
  }
  renderWithProviders(
    <AuthContext.Provider value={auth}>
      <AppHeader />
    </AuthContext.Provider>,
    route,
  )
}

const user = { id: '1', email: 'a@b.c', name: 'Somchai', role: 'user' as const }

it('shows Products, Login and Register for a guest', () => {
  renderHeader({ status: 'guest' })

  expect(screen.getByRole('link', { name: 'เข้าสู่ระบบ' })).toBeInTheDocument()
  expect(screen.getByRole('link', { name: 'สมัครสมาชิก' })).toBeInTheDocument()
  expect(screen.queryByRole('link', { name: 'คำสั่งซื้อของฉัน' })).not.toBeInTheDocument()
  expect(screen.queryByRole('link', { name: 'ผู้ดูแลระบบ' })).not.toBeInTheDocument()
})

it('shows the user name, My Orders and Logout for a user', () => {
  renderHeader({ status: 'authed', user })

  expect(screen.getByText('Somchai')).toBeInTheDocument()
  expect(screen.getByRole('link', { name: 'คำสั่งซื้อของฉัน' })).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'ออกจากระบบ' })).toBeInTheDocument()
  expect(screen.queryByRole('link', { name: 'ผู้ดูแลระบบ' })).not.toBeInTheDocument()
})

it('links an admin to the admin area', () => {
  renderHeader({ status: 'authed', user: { ...user, role: 'admin' } })

  expect(screen.getByRole('link', { name: 'ผู้ดูแลระบบ' })).toHaveAttribute('href', '/admin/products')
})

it('highlights the active nav link', () => {
  renderHeader({ status: 'authed', user }, '/orders')

  expect(screen.getByRole('link', { name: 'คำสั่งซื้อของฉัน' })).toHaveClass('active')
})
