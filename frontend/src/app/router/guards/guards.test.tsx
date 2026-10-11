import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router'
import { expect, it } from 'vitest'
import { AuthContext, type AuthContextValue } from '@/features/auth'
import { GuestOnly } from './GuestOnly'
import { RequireAuth } from './RequireAuth'

function authValue(overrides: Partial<AuthContextValue>): AuthContextValue {
  return {
    status: 'guest',
    user: null,
    sessionExpired: false,
    login: async () => {},
    logout: async () => {},
    ...overrides,
  }
}

function LocationProbe({ label }: { label: string }) {
  const location = useLocation()
  return (
    <div>
      {label}:{location.pathname}
      {location.search}
    </div>
  )
}

function renderAt(url: string, value: AuthContextValue) {
  render(
    <AuthContext.Provider value={value}>
      <MemoryRouter initialEntries={[url]}>
        <Routes>
          <Route path="/login" element={<LocationProbe label="login" />} />
          <Route path="/403" element={<LocationProbe label="forbidden" />} />
          <Route path="/products" element={<LocationProbe label="products" />} />
          <Route element={<GuestOnly />}>
            <Route path="/guest-page" element={<LocationProbe label="guest-page" />} />
          </Route>
          <Route element={<RequireAuth />}>
            <Route path="/orders" element={<LocationProbe label="orders" />} />
          </Route>
          <Route element={<RequireAuth role="admin" />}>
            <Route path="/admin" element={<LocationProbe label="admin" />} />
          </Route>
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  )
}

const user = { id: '1', email: 'a@b.c', name: 'A', role: 'user' as const }
const admin = { ...user, role: 'admin' as const }

it('shows a loader while the session is being restored', () => {
  renderAt('/orders', authValue({ status: 'loading' }))

  expect(screen.getByRole('progressbar')).toBeInTheDocument()
})

it('sends a guest to login with returnTo', () => {
  renderAt('/orders?page=2', authValue({ status: 'guest' }))

  expect(screen.getByText('login:/login?returnTo=%2Forders%3Fpage%3D2')).toBeInTheDocument()
})

it('marks the login redirect when the session expired', () => {
  renderAt('/orders', authValue({ status: 'guest', sessionExpired: true }))

  expect(screen.getByText('login:/login?returnTo=%2Forders&expired=1')).toBeInTheDocument()
})

it('lets a signed-in user reach a protected page', () => {
  renderAt('/orders', authValue({ status: 'authed', user }))

  expect(screen.getByText('orders:/orders')).toBeInTheDocument()
})

it('sends a plain user away from an admin page to 403', () => {
  renderAt('/admin', authValue({ status: 'authed', user }))

  expect(screen.getByText('forbidden:/403')).toBeInTheDocument()
})

it('lets an admin into an admin page', () => {
  renderAt('/admin', authValue({ status: 'authed', user: admin }))

  expect(screen.getByText('admin:/admin')).toBeInTheDocument()
})

it('keeps a guest on a guest-only page', () => {
  renderAt('/guest-page', authValue({ status: 'guest' }))
  expect(screen.getByText('guest-page:/guest-page')).toBeInTheDocument()
})

it('moves a signed-in user from a guest-only page to returnTo', () => {
  renderAt('/guest-page?returnTo=%2Forders', authValue({ status: 'authed', user }))

  expect(screen.getByText('orders:/orders')).toBeInTheDocument()
})

it('ignores an external returnTo and falls back to /products', () => {
  renderAt('/guest-page?returnTo=%2F%2Fevil.com', authValue({ status: 'authed', user }))

  expect(screen.getByText('products:/products')).toBeInTheDocument()
})
