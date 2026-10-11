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
  await signIn('user')
})
afterEach(() => server.resetHandlers())
afterAll(() => server.close())

it.each([
  ['Pending', 'Reserving', true],
  ['Pending', 'Reserved', true],
  ['Confirmed', null, false],
  ['Rejected', null, false],
  ['Cancelled', null, false],
] as const)('%s / %s: cancel button shown = %s', async (status, stockStatus, shown) => {
  const order = seedOrder({ status, stockStatus })
  renderApp(`/orders/${order.id}`)

  await screen.findByText('คำสั่งซื้อ')

  expect(screen.queryByRole('button', { name: 'ยกเลิกคำสั่งซื้อ' }) !== null).toBe(shown)
})

it('cancels after confirmation and shows the Cancelled status', async () => {
  const order = seedOrder()
  renderApp(`/orders/${order.id}`)

  await userEvent.click(await screen.findByRole('button', { name: 'ยกเลิกคำสั่งซื้อ' }))
  await userEvent.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'ยืนยันยกเลิก' }))

  expect(await screen.findByText('ยกเลิกคำสั่งซื้อแล้ว')).toBeInTheDocument()
  await waitFor(() => expect(screen.queryByRole('button', { name: 'ยกเลิกคำสั่งซื้อ' })).not.toBeInTheDocument())
  expect(db.orders.find((o) => o.id === order.id)?.status).toBe('Cancelled')
})

it('shows a message and the real status on 409 CANNOT_CANCEL', async () => {
  const order = seedOrder()
  renderApp(`/orders/${order.id}`)
  await userEvent.click(await screen.findByRole('button', { name: 'ยกเลิกคำสั่งซื้อ' }))
  db.orders.find((o) => o.id === order.id)!.status = 'Confirmed'

  await userEvent.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'ยืนยันยกเลิก' }))

  expect(await screen.findByText(/ยกเลิกไม่ได้แล้ว/)).toBeInTheDocument()
  expect(await screen.findAllByText('ยืนยันแล้ว')).not.toHaveLength(0)
})
