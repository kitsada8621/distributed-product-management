import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { afterAll, afterEach, beforeAll, beforeEach, expect, it } from 'vitest'
import { db } from '@/mocks/db'
import { apiUrl } from '@/mocks/http'
import { server } from '@/mocks/server'
import { renderApp } from '@/test/renderApp'
import { resetTestState, signIn } from '@/test/session'

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
beforeEach(async () => {
  resetTestState()
  await signIn('admin')
})
afterEach(() => server.resetHandlers())
afterAll(() => server.close())

async function chooseCategory(name: string) {
  await userEvent.click(await screen.findByRole('combobox', { name: 'หมวดหมู่' }))
  await userEvent.click(await screen.findByRole('option', { name }))
}

it('shows validation messages on the client', async () => {
  renderApp('/admin/products/new')

  await userEvent.click(await screen.findByRole('button', { name: 'บันทึก' }))

  expect(await screen.findByText('Name is required')).toBeInTheDocument()
  expect(screen.getByText('Price must be greater than 0')).toBeInTheDocument()
  expect(screen.getByText('Select a category')).toBeInTheDocument()
})

it('creates a product with its initial stock and returns to the list', async () => {
  const { router } = renderApp('/admin/products/new')

  await userEvent.type(await screen.findByLabelText('ชื่อสินค้า'), 'Gamepad')
  await userEvent.type(screen.getByLabelText('ราคา'), '799')
  await chooseCategory('Electronics')
  await userEvent.clear(screen.getByLabelText('จำนวนสต็อกใหม่ (แทนที่ค่าปัจจุบัน)'))
  await userEvent.type(screen.getByLabelText('จำนวนสต็อกใหม่ (แทนที่ค่าปัจจุบัน)'), '12')
  await userEvent.click(screen.getByRole('button', { name: 'บันทึก' }))

  await waitFor(() => expect(router.state.location.pathname).toBe('/admin/products'))
  expect(db.products.find((p) => p.name === 'Gamepad')).toMatchObject({ price: 799, stock: 12 })
})

it('edits the product and then its stock', async () => {
  const { router } = renderApp('/admin/products/p-2')

  const name = await screen.findByLabelText('ชื่อสินค้า')
  await waitFor(() => expect(name).toHaveValue('Mouse'))
  await userEvent.clear(name)
  await userEvent.type(name, 'Mouse X')
  await userEvent.clear(screen.getByLabelText('จำนวนสต็อกใหม่ (แทนที่ค่าปัจจุบัน)'))
  await userEvent.type(screen.getByLabelText('จำนวนสต็อกใหม่ (แทนที่ค่าปัจจุบัน)'), '99')
  await userEvent.click(screen.getByRole('button', { name: 'บันทึก' }))

  await waitFor(() => expect(router.state.location.pathname).toBe('/admin/products'))
  expect(db.products.find((p) => p.id === 'p-2')).toMatchObject({ name: 'Mouse X', stock: 99 })
})

it('reports a partial failure: product saved, stock failed', async () => {
  server.use(
    http.put(apiUrl('/api/catalog/products/:id/stock'), () =>
      HttpResponse.json({ status: 422, code: 'STOCK_NEGATIVE', message: 'Stock cannot be negative', data: null }, { status: 422 }),
    ),
  )
  const { router } = renderApp('/admin/products/p-2')

  const name = await screen.findByLabelText('ชื่อสินค้า')
  await waitFor(() => expect(name).toHaveValue('Mouse'))
  await userEvent.clear(name)
  await userEvent.type(name, 'Mouse Y')
  await userEvent.clear(screen.getByLabelText('จำนวนสต็อกใหม่ (แทนที่ค่าปัจจุบัน)'))
  await userEvent.type(screen.getByLabelText('จำนวนสต็อกใหม่ (แทนที่ค่าปัจจุบัน)'), '50')
  await userEvent.click(screen.getByRole('button', { name: 'บันทึก' }))

  expect(await screen.findByText('บันทึกข้อมูลสินค้าแล้ว')).toBeInTheDocument()
  expect(screen.getByText('สต็อกไม่ได้บันทึก: Stock cannot be negative')).toBeInTheDocument()
  expect(router.state.location.pathname).toBe('/admin/products/p-2')
  expect(db.products.find((p) => p.id === 'p-2')?.name).toBe('Mouse Y')
})

it('does not send stock when it is unchanged', async () => {
  let stockCalls = 0
  server.events.on('request:start', ({ request }) => {
    if (request.url.endsWith('/stock')) stockCalls += 1
  })
  const { router } = renderApp('/admin/products/p-2')

  const name = await screen.findByLabelText('ชื่อสินค้า')
  await waitFor(() => expect(name).toHaveValue('Mouse'))
  await userEvent.click(screen.getByRole('button', { name: 'บันทึก' }))

  await waitFor(() => expect(router.state.location.pathname).toBe('/admin/products'))
  expect(stockCalls).toBe(0)
  server.events.removeAllListeners()
})

it('shows not found when editing a deleted product', async () => {
  renderApp('/admin/products/missing')

  expect(await screen.findByText('ไม่พบสินค้า')).toBeInTheDocument()
})

it('edit mode shows product info and stock as two cards with one save button', async () => {
  await signIn('admin')
  renderApp('/admin/products/p-2')

  expect(await screen.findByText('ข้อมูลสินค้า')).toBeInTheDocument()
  expect(screen.getByText('สต็อก')).toBeInTheDocument()
  expect(screen.getAllByRole('button', { name: 'บันทึก' })).toHaveLength(1)
})
