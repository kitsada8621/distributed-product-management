import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { afterAll, afterEach, beforeAll, beforeEach, expect, it } from 'vitest'
import { useOrderDraftStore } from '@/features/orders'
import { db, expireAccessTokens } from '@/mocks/db'
import { apiUrl } from '@/mocks/http'
import { server } from '@/mocks/server'
import { fillDelivery } from './fillDelivery'
import { renderApp } from './renderApp'
import { seedOrder } from './seedOrder'
import { resetTestState, signIn } from './session'

let refreshCalls = 0

beforeAll(() => {
  server.listen({ onUnhandledRequest: 'error' })
  server.events.on('request:start', ({ request }) => {
    if (request.url.endsWith('/api/auth/refresh')) refreshCalls += 1
  })
})
beforeEach(() => {
  resetTestState()
  useOrderDraftStore.getState().clear()
  db.reserveDelayMs = 0
  refreshCalls = 0
})
afterEach(() => server.resetHandlers())
afterAll(() => server.close())

it('order flow: user orders a product, admin confirms it, status becomes Confirmed', async () => {
  await signIn('user')
  const userApp = renderApp('/products?q=Monitor')

  await userEvent.click(await screen.findByRole('button', { name: 'เพิ่มในรายการ' }))
  await fillDelivery()
  await userEvent.click(await screen.findByRole('button', { name: 'ยืนยันคำสั่งซื้อ' }))
  await waitFor(() => expect(userApp.router.state.location.pathname).toMatch(/^\/orders\/[0-9a-f-]{36}$/))
  const orderId = userApp.router.state.location.pathname.split('/').pop()!
  expect(await screen.findByText('รอการยืนยัน')).toBeInTheDocument()
  userApp.unmount()

  resetAuthKeepData()
  await signIn('admin')
  renderApp(`/admin/orders/${orderId}`)
  await userEvent.click(await screen.findByRole('button', { name: 'ยืนยันคำสั่งซื้อ' }))
  await waitFor(() => expect(db.orders.find((o) => o.id === orderId)?.status).toBe('Confirmed'))
})

function resetAuthKeepData() {
  document.cookie = 'csrfToken=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/'
}

it('refreshes the access token once when it expires during use, then retries', async () => {
  await signIn('user')
  const order = seedOrder()
  renderApp('/orders')
  const link = await screen.findByRole('link', { name: order.id.slice(0, 8) })
  refreshCalls = 0

  expireAccessTokens()
  await userEvent.click(link)

  expect(await screen.findByText('คำสั่งซื้อ')).toBeInTheDocument()
  expect(await screen.findByText('รอการยืนยัน')).toBeInTheDocument()
  expect(refreshCalls).toBe(1)
})

it('sends the user to login with the expired flag when refresh fails', async () => {
  await signIn('user')
  const order = seedOrder()
  const { router } = renderApp('/orders')
  const link = await screen.findByRole('link', { name: order.id.slice(0, 8) })
  server.use(
    http.post(apiUrl('/api/auth/refresh'), () =>
      HttpResponse.json({ status: 401, code: 'REFRESH_INVALID', message: 'Session expired', data: null }, { status: 401 }),
    ),
  )

  expireAccessTokens()
  await userEvent.click(link)

  await waitFor(() => expect(router.state.location.pathname).toBe('/login'))
  expect(router.state.location.search).toContain('expired=1')
  expect(await screen.findByText('เซสชันหมดอายุ กรุณาเข้าสู่ระบบอีกครั้ง')).toBeInTheDocument()
})

it('401: a guest opening a protected page goes to login', async () => {
  const { router } = renderApp('/orders')

  await waitFor(() => expect(router.state.location.pathname).toBe('/login'))
})

it('403: a plain user opening an admin page sees the access denied page', async () => {
  await signIn('user')
  renderApp('/admin/products')

  expect(await screen.findByRole('heading', { name: 'ไม่มีสิทธิ์เข้าถึง' })).toBeInTheDocument()
})

it('404: an unknown path shows the not found page', async () => {
  renderApp('/no/such/page')

  expect(await screen.findByRole('heading', { name: 'ไม่พบหน้าที่ต้องการ' })).toBeInTheDocument()
})

it('409: a user cannot cancel an order that is already confirmed', async () => {
  await signIn('user')
  const order = seedOrder()
  renderApp(`/orders/${order.id}`)
  await userEvent.click(await screen.findByRole('button', { name: 'ยกเลิกคำสั่งซื้อ' }))
  const stored = db.orders.find((o) => o.id === order.id)!
  stored.status = 'Confirmed'
  stored.stockStatus = null

  await userEvent.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'ยืนยันยกเลิก' }))

  expect(await screen.findByText(/ยกเลิกไม่ได้แล้ว/)).toBeInTheDocument()
})

it('stock shortage: the rejected order explains what was short', async () => {
  await signIn('user')
  useOrderDraftStore.getState().add({ productId: 'p-3', name: 'Monitor', unitPrice: 200 }, 99)
  const { router } = renderApp('/orders/new')

  await fillDelivery()
  await userEvent.click(await screen.findByRole('button', { name: 'ยืนยันคำสั่งซื้อ' }))

  await waitFor(() => expect(router.state.location.pathname).toMatch(/^\/orders\/[0-9a-f-]{36}$/))
  expect(await screen.findByText('Monitor: ขอ 99 ชิ้น มีเพียง 7 ชิ้น')).toBeInTheDocument()
})

it('deleting a category that still has products leaves them showing ไม่มีหมวดหมู่', async () => {
  await signIn('admin')
  const categories = renderApp('/admin/categories')
  await userEvent.click(await screen.findByRole('button', { name: 'ลบ Books' }))
  await userEvent.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'ลบ' }))
  await screen.findByText('ลบหมวดหมู่แล้ว')
  categories.unmount()

  renderApp('/products')

  expect((await screen.findAllByText('ไม่มีหมวดหมู่')).length).toBeGreaterThan(0)
})
