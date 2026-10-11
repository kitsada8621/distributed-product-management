import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterAll, afterEach, beforeAll, beforeEach, expect, it, vi } from 'vitest'
import { server } from '@/mocks/server'
import { renderApp } from '@/test/renderApp'
import { seedOrder } from '@/test/seedOrder'
import { resetTestState, signIn } from '@/test/session'

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
beforeEach(async () => {
  resetTestState()
  await signIn('user')
})
afterEach(() => server.resetHandlers())
afterAll(() => server.close())

it('lists only my orders with a status and a link to the detail', async () => {
  const mine = seedOrder()
  seedOrder({ userId: 'u-admin', ownerEmail: 'admin@example.com' })
  renderApp('/orders')

  const link = await screen.findByRole('link', { name: mine.id.slice(0, 8) })

  expect(link).toHaveAttribute('href', `/orders/${mine.id}`)
  expect(screen.getAllByRole('row')).toHaveLength(2)
  expect(screen.getByText('รอการยืนยัน')).toBeInTheDocument()
})

it('filters by status and keeps the filter in the URL', async () => {
  const pending = seedOrder()
  const confirmed = seedOrder({ status: 'Confirmed', stockStatus: null })
  const { router } = renderApp('/orders')
  await screen.findByRole('link', { name: pending.id.slice(0, 8) })

  await userEvent.click(screen.getByRole('combobox', { name: 'สถานะ' }))
  await userEvent.click(await screen.findByRole('option', { name: 'ยืนยันแล้ว' }))

  await waitFor(() => expect(router.state.location.search).toBe('?status=Confirmed'))
  await waitFor(() => expect(screen.getAllByRole('row')).toHaveLength(2))
  expect(screen.getByRole('link', { name: confirmed.id.slice(0, 8) })).toBeInTheDocument()
  expect(screen.queryByRole('link', { name: pending.id.slice(0, 8) })).not.toBeInTheDocument()
})

it('paginates after 20 orders', async () => {
  for (let i = 0; i < 25; i += 1) {
    seedOrder({ createdAt: new Date(Date.UTC(2026, 0, 1, 0, i)).toISOString() })
  }
  const { router } = renderApp('/orders')
  await screen.findAllByRole('link')
  expect(await screen.findAllByRole('row')).toHaveLength(21)

  await userEvent.click(screen.getByRole('button', { name: /page 2/i }))

  await waitFor(() => expect(router.state.location.search).toBe('?page=2'))
  await waitFor(() => expect(screen.getAllByRole('row')).toHaveLength(6))
})

it('shows an empty state linking to products', async () => {
  renderApp('/orders')

  expect(await screen.findByText('ยังไม่มีคำสั่งซื้อ')).toBeInTheDocument()
  expect(screen.getByRole('link', { name: 'เลือกซื้อสินค้า' })).toHaveAttribute('href', '/products')
})

it('sends a guest to login', async () => {
  resetTestState()
  const { router } = renderApp('/orders')

  await waitFor(() => expect(router.state.location.pathname).toBe('/login'))
})

it('renders cards instead of a table on narrow screens', async () => {
  const mine = seedOrder()
  vi.stubGlobal('matchMedia', (query: string) => ({
    matches: true,
    media: query,
    addEventListener: () => {},
    removeEventListener: () => {},
    addListener: () => {},
    removeListener: () => {},
    onchange: null,
    dispatchEvent: () => false,
  }))
  try {
    renderApp('/orders')

    expect(await screen.findByRole('link', { name: mine.id.slice(0, 8) })).toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
  } finally {
    vi.unstubAllGlobals()
  }
})
