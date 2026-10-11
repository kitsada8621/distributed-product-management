import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterAll, afterEach, beforeAll, beforeEach, expect, it } from 'vitest'
import { server } from '@/mocks/server'
import { renderApp } from '@/test/renderApp'
import { seedOrder } from '@/test/seedOrder'
import { resetTestState, signIn } from '@/test/session'

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
beforeEach(() => resetTestState())
afterEach(() => server.resetHandlers())
afterAll(() => server.close())

it('sends a plain user to 403', async () => {
  await signIn('user')
  const { router } = renderApp('/admin/orders')

  await screen.findByRole('heading', { name: 'ไม่มีสิทธิ์เข้าถึง' })

  expect(router.state.location.pathname).toBe('/403')
})

it('lists every order with the owner email and links to the admin detail', async () => {
  await signIn('admin')
  const order = seedOrder()
  seedOrder({ userId: 'u-admin', ownerEmail: 'admin@example.com', status: 'Confirmed', stockStatus: null })
  renderApp('/admin/orders')

  const link = await screen.findByRole('link', { name: order.id.slice(0, 8) })

  expect(link).toHaveAttribute('href', `/admin/orders/${order.id}`)
  expect(screen.getByText('user@example.com')).toBeInTheDocument()
  expect(screen.getByText('admin@example.com')).toBeInTheDocument()
})

it('filters by status and keeps the filter in the URL', async () => {
  await signIn('admin')
  seedOrder()
  seedOrder({ status: 'Confirmed', stockStatus: null, ownerEmail: 'confirmed@example.com' })
  const { router } = renderApp('/admin/orders')
  await screen.findByText('confirmed@example.com')

  await userEvent.click(screen.getByRole('combobox', { name: 'สถานะ' }))
  await userEvent.click(await screen.findByRole('option', { name: 'ยืนยันแล้ว' }))

  await waitFor(() => expect(router.state.location.search).toBe('?status=Confirmed'))
  await waitFor(() => expect(screen.getAllByRole('row')).toHaveLength(2))
  expect(screen.getByText('confirmed@example.com')).toBeInTheDocument()
})

it('shows an empty state when no order matches', async () => {
  await signIn('admin')
  renderApp('/admin/orders?status=Rejected')

  expect(await screen.findByText('ไม่มีคำสั่งซื้อในสถานะนี้')).toBeInTheDocument()
})
