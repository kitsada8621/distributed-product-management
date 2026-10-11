import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { afterAll, afterEach, beforeAll, beforeEach, expect, it } from 'vitest'
import { useOrderDraftStore } from '@/features/orders'
import { db } from '@/mocks/db'
import { apiUrl } from '@/mocks/http'
import { server } from '@/mocks/server'
import { renderApp } from '@/test/renderApp'
import { fillDelivery } from '@/test/fillDelivery'
import { resetTestState, signIn } from '@/test/session'

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
beforeEach(async () => {
  resetTestState()
  useOrderDraftStore.getState().clear()
  await signIn('user')
})
afterEach(() => server.resetHandlers())
afterAll(() => server.close())

const mouse = { productId: 'p-2', name: 'Mouse', unitPrice: 150 }

const confirmButton = () => screen.findByRole('button', { name: 'ยืนยันคำสั่งซื้อ' })

it('asks the user to pick products when the draft is empty', async () => {
  renderApp('/orders/new')

  expect(await screen.findByText('ยังไม่มีสินค้าในรายการ')).toBeInTheDocument()
  expect(screen.getByRole('link', { name: 'เลือกสินค้า' })).toHaveAttribute('href', '/products')
})

it('shows the items and an estimated total labelled as an estimate', async () => {
  useOrderDraftStore.getState().add(mouse, 2)
  renderApp('/orders/new')

  expect(await screen.findByText('Mouse')).toBeInTheDocument()
  expect(screen.getByText('ยอดรวมโดยประมาณ')).toBeInTheDocument()
  expect(screen.getByText('฿300.00', { selector: 'h5' })).toBeInTheDocument()
  expect(screen.getByText('ยอดรวมสุดท้ายจะยืนยันหลังตรวจสอบสต็อก')).toBeInTheDocument()
})

it('disables confirm for an invalid quantity', async () => {
  useOrderDraftStore.getState().add(mouse, 1)
  renderApp('/orders/new')

  const quantity = await screen.findByLabelText('จำนวน Mouse')
  await userEvent.clear(quantity)

  expect(await screen.findByText('กรอกจำนวนเต็มที่มากกว่า 0')).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'ยืนยันคำสั่งซื้อ' })).toBeDisabled()
})

it('places the order, clears the draft and opens the order page', async () => {
  useOrderDraftStore.getState().add(mouse, 2)
  const { router } = renderApp('/orders/new')

  await fillDelivery()
  await userEvent.click(await confirmButton())

  await waitFor(() => expect(router.state.location.pathname).toMatch(/^\/orders\/[0-9a-f-]{36}$/))
  expect(useOrderDraftStore.getState().items).toEqual([])
  expect(db.orders).toHaveLength(1)
})

it('shows errors and sends nothing when the delivery form is empty', async () => {
  useOrderDraftStore.getState().add(mouse, 1)
  renderApp('/orders/new')

  await userEvent.click(await confirmButton())

  expect(await screen.findAllByText('กรุณากรอกข้อมูล')).toHaveLength(5)
  expect(screen.getByText('เลือกวิธีชำระเงิน')).toBeInTheDocument()
  expect(db.orders).toHaveLength(0)
})

it('offers only cash on delivery and pay at store', async () => {
  useOrderDraftStore.getState().add(mouse, 1)
  renderApp('/orders/new')

  await userEvent.click(await screen.findByRole('combobox', { name: 'วิธีชำระเงิน' }))

  const options = within(await screen.findByRole('listbox')).getAllByRole('option')
  expect(options.map((option) => option.textContent)).toEqual(['เก็บเงินปลายทาง', 'ชำระหน้าร้าน'])
})

it('sends the address and the chosen payment method', async () => {
  useOrderDraftStore.getState().add(mouse, 1)
  let sentBody: Record<string, unknown> = {}
  server.use(
    http.post(apiUrl('/api/orders'), async ({ request }) => {
      sentBody = (await request.json()) as Record<string, unknown>
      return HttpResponse.json(
        { status: 202, success: true, code: 'SUCCESS', message: '', data: { orderId: 'o-1', status: 'Pending', stockStatus: 'Reserving' }, meta: null, errors: null },
        { status: 202 },
      )
    }),
  )
  renderApp('/orders/new')

  await fillDelivery({ payment: 'ชำระหน้าร้าน' })
  await userEvent.click(await confirmButton())

  await waitFor(() => expect(sentBody.paymentMethod).toBe('PAY_AT_STORE'))
  expect(sentBody.shippingAddress).toEqual({
    recipientName: 'Somchai Jaidee',
    phone: '0812345678',
    addressLine: '99/9 Soi Sukhumvit 21',
    province: 'Bangkok',
    postalCode: '10110',
  })
})

it('shows a server 422 under the matching input', async () => {
  useOrderDraftStore.getState().add(mouse, 1)
  server.use(
    http.post(apiUrl('/api/orders'), () =>
      HttpResponse.json(
        {
          status: 422,
          success: false,
          code: 'VALIDATION_ERROR',
          message: 'Invalid input',
          data: null,
          meta: null,
          errors: [{ field: 'shippingAddress.phone', code: 'INVALID_FORMAT', message: 'เบอร์โทรนี้ใช้ไม่ได้' }],
        },
        { status: 422 },
      ),
    ),
  )
  renderApp('/orders/new')

  await fillDelivery()
  await userEvent.click(await confirmButton())

  expect(await screen.findByText('เบอร์โทรนี้ใช้ไม่ได้')).toBeInTheDocument()
  expect(screen.getByLabelText('เบอร์โทรศัพท์')).toHaveAttribute('aria-invalid', 'true')
})

it('uses a new Idempotency-Key after the address is edited following a failure', async () => {
  useOrderDraftStore.getState().add(mouse, 1)
  const keys: (string | null)[] = []
  server.use(
    http.post(apiUrl('/api/orders'), ({ request }) => {
      keys.push(request.headers.get('Idempotency-Key'))
      return HttpResponse.error()
    }),
  )
  renderApp('/orders/new')

  await fillDelivery()
  await userEvent.click(await confirmButton())
  expect(await screen.findByText(/ติดต่อเซิร์ฟเวอร์ไม่ได้/)).toBeInTheDocument()
  await userEvent.type(screen.getByLabelText(/ที่อยู่ \(บ้านเลขที่/), ' Building B')
  await userEvent.click(screen.getByRole('button', { name: 'ลองใหม่' }))

  await waitFor(() => expect(keys).toHaveLength(2))
  expect(keys[1]).not.toBe(keys[0])
})

it('sends once even when the button is double clicked', async () => {
  useOrderDraftStore.getState().add(mouse, 1)
  let posts = 0
  server.events.on('request:start', ({ request }) => {
    if (request.method === 'POST' && request.url.endsWith('/api/orders')) posts += 1
  })
  renderApp('/orders/new')

  await fillDelivery()
  await userEvent.dblClick(await confirmButton())

  await waitFor(() => expect(db.orders).toHaveLength(1))
  expect(posts).toBe(1)
  server.events.removeAllListeners()
})

it('retries with the same key after a network failure', async () => {
  useOrderDraftStore.getState().add(mouse, 1)
  const keys: (string | null)[] = []
  let failNext = true
  server.use(
    http.post(apiUrl('/api/orders'), ({ request }) => {
      keys.push(request.headers.get('Idempotency-Key'))
      if (failNext) {
        failNext = false
        return HttpResponse.error()
      }
      return HttpResponse.json(
        { status: 202, success: true, code: 'SUCCESS', message: '', data: { orderId: 'o-1', status: 'Pending', stockStatus: 'Reserving' }, meta: null, errors: null },
        { status: 202 },
      )
    }),
  )
  renderApp('/orders/new')

  await fillDelivery()
  await userEvent.click(await confirmButton())
  expect(await screen.findByText(/ติดต่อเซิร์ฟเวอร์ไม่ได้/)).toBeInTheDocument()
  await userEvent.click(screen.getByRole('button', { name: 'ลองใหม่' }))

  await waitFor(() => expect(keys).toHaveLength(2))
  expect(keys[0]).toBeTruthy()
  expect(keys[1]).toBe(keys[0])
})

it('shows the number of items and the summed quantity in the summary', async () => {
  useOrderDraftStore.getState().add(mouse, 2)
  useOrderDraftStore.getState().add({ productId: 'p-3', name: 'Monitor', unitPrice: 200 }, 3)
  renderApp('/orders/new')

  const pieces = await screen.findByText('จำนวนชิ้นรวม')

  expect(pieces.nextElementSibling).toHaveTextContent('5')
  expect(screen.getByText('จำนวนรายการ').nextElementSibling).toHaveTextContent('2')
})
