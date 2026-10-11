import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterAll, afterEach, beforeAll, beforeEach, expect, it } from 'vitest'
import { db } from '@/mocks/db'
import { server } from '@/mocks/server'
import { renderApp } from '@/test/renderApp'
import { seedOrder } from '@/test/seedOrder'
import { resetTestState, signIn } from '@/test/session'

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
beforeEach(async () => {
  resetTestState()
  await signIn('admin')
})
afterEach(() => server.resetHandlers())
afterAll(() => server.close())

it.each([
  ['Pending', 'Reserved', true],
  ['Pending', 'Reserving', false],
  ['Confirmed', null, false],
  ['Rejected', null, false],
  ['Cancelled', null, false],
] as const)('%s / %s: confirm and reject shown = %s', async (status, stockStatus, shown) => {
  const order = seedOrder({ status, stockStatus })
  renderApp(`/admin/orders/${order.id}`)

  await screen.findByText(/user@example.com/)

  expect(screen.queryByRole('button', { name: 'ยืนยันคำสั่งซื้อ' }) !== null).toBe(shown)
  expect(screen.queryByRole('button', { name: 'ปฏิเสธคำสั่งซื้อ' }) !== null).toBe(shown)
})

it('confirms a reserved order', async () => {
  const order = seedOrder()
  renderApp(`/admin/orders/${order.id}`)

  await userEvent.click(await screen.findByRole('button', { name: 'ยืนยันคำสั่งซื้อ' }))

  expect(await screen.findByText('ยืนยันคำสั่งซื้อแล้ว')).toBeInTheDocument()
  await waitFor(() => expect(screen.queryByRole('button', { name: 'ยืนยันคำสั่งซื้อ' })).not.toBeInTheDocument())
  expect(db.orders.find((o) => o.id === order.id)?.status).toBe('Confirmed')
})

it('requires a reason to reject', async () => {
  const order = seedOrder()
  renderApp(`/admin/orders/${order.id}`)

  await userEvent.click(await screen.findByRole('button', { name: 'ปฏิเสธคำสั่งซื้อ' }))
  await userEvent.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'ยืนยันปฏิเสธ' }))

  expect(await screen.findByText('กรุณาระบุเหตุผล')).toBeInTheDocument()
  expect(db.orders.find((o) => o.id === order.id)?.status).toBe('Pending')
})

it('rejects with the entered reason and shows it', async () => {
  const order = seedOrder()
  renderApp(`/admin/orders/${order.id}`)

  await userEvent.click(await screen.findByRole('button', { name: 'ปฏิเสธคำสั่งซื้อ' }))
  const dialog = await screen.findByRole('dialog')
  await userEvent.type(within(dialog).getByLabelText('เหตุผล'), 'Item damaged')
  await userEvent.click(within(dialog).getByRole('button', { name: 'ยืนยันปฏิเสธ' }))

  expect(await screen.findByText('ผู้ดูแลระบบปฏิเสธ: Item damaged')).toBeInTheDocument()
  expect(db.orders.find((o) => o.id === order.id)?.rejection).toMatchObject({ type: 'ADMIN_REJECTED' })
})

it('shows a message on 409 INVALID_STATE and refreshes the order', async () => {
  const order = seedOrder()
  renderApp(`/admin/orders/${order.id}`)
  const button = await screen.findByRole('button', { name: 'ยืนยันคำสั่งซื้อ' })
  const stored = db.orders.find((o) => o.id === order.id)!
  stored.status = 'Cancelled'
  stored.stockStatus = null

  await userEvent.click(button)

  expect(await screen.findByText(/สถานะคำสั่งซื้อเปลี่ยนแล้ว/)).toBeInTheDocument()
  await waitFor(() => expect(screen.queryByRole('button', { name: 'ยืนยันคำสั่งซื้อ' })).not.toBeInTheDocument())
})

it('shows not found for an unknown order', async () => {
  renderApp('/admin/orders/missing')

  expect(await screen.findByText('ไม่พบคำสั่งซื้อ')).toBeInTheDocument()
})

it('renders reject as an outlined error button and confirm as primary', async () => {
  const order = seedOrder()
  renderApp(`/admin/orders/${order.id}`)

  const reject = await screen.findByRole('button', { name: 'ปฏิเสธคำสั่งซื้อ' })
  expect(reject).toHaveClass('MuiButton-outlined', 'MuiButton-colorError')
  expect(screen.getByRole('button', { name: 'ยืนยันคำสั่งซื้อ' })).toHaveClass('MuiButton-contained', 'MuiButton-colorPrimary')
})
