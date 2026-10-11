import { render, screen } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { expect, it } from 'vitest'
import { AuthContext } from '@/features/auth'
import { routes } from '@/app/router/routes'

const guest = { status: 'guest' as const, user: null, sessionExpired: false, login: async () => {}, logout: async () => {} }

function renderAt(entry: string | { pathname: string; state: unknown }) {
  const router = createMemoryRouter(routes, { initialEntries: [entry] })
  render(
    <AuthContext.Provider value={guest}>
      <RouterProvider router={router} />
    </AuthContext.Provider>,
  )
}

it('renders the 404 page for an unknown path', async () => {
  renderAt('/definitely/not/here')

  expect(await screen.findByRole('heading', { name: 'ไม่พบหน้าที่ต้องการ' })).toBeInTheDocument()
  expect(screen.getByRole('link', { name: 'กลับไปหน้าสินค้า' })).toHaveAttribute('href', '/products')
})

it('shows the 404 code on the not-found page', async () => {
  renderAt('/definitely/not/here')

  expect(await screen.findByText('404')).toBeInTheDocument()
})

it('renders the 403 page with a link back to products', async () => {
  renderAt('/403')

  expect(await screen.findByRole('heading', { name: 'ไม่มีสิทธิ์เข้าถึง' })).toBeInTheDocument()
  expect(screen.getByRole('link', { name: 'กลับไปหน้าสินค้า' })).toHaveAttribute('href', '/products')
})

it('renders the 401 page linking to login with returnTo', async () => {
  renderAt({ pathname: '/401', state: { returnTo: '/orders' } })

  expect(await screen.findByRole('heading', { name: 'กรุณาเข้าสู่ระบบ' })).toBeInTheDocument()
  expect(screen.getByRole('link', { name: 'ไปหน้าเข้าสู่ระบบ' })).toHaveAttribute('href', '/login?returnTo=%2Forders')
})

it('shows the correlation id when present', async () => {
  renderAt({ pathname: '/403', state: { correlationId: 'cid-77' } })

  expect(await screen.findByText('cid-77')).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'คัดลอกรหัสอ้างอิง' })).toBeInTheDocument()
})
