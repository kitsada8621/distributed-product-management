import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterAll, afterEach, beforeAll, beforeEach, expect, it } from 'vitest'
import { db } from '@/mocks/db'
import { server } from '@/mocks/server'
import { renderApp } from '@/test/renderApp'
import { resetTestState, signIn } from '@/test/session'

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
beforeEach(() => resetTestState())
afterEach(() => server.resetHandlers())
afterAll(() => server.close())

it('sends a plain user to the 403 page and a guest to login', async () => {
  await signIn('user')
  const first = renderApp('/admin/products')
  await screen.findByRole('heading', { name: 'ไม่มีสิทธิ์เข้าถึง' })
  expect(first.router.state.location.pathname).toBe('/403')
  first.unmount()

  resetTestState()
  const second = renderApp('/admin/products')
  await waitFor(() => expect(second.router.state.location.pathname).toBe('/login'))
  expect(second.router.state.location.search).toBe('?returnTo=%2Fadmin%2Fproducts')
})

it('renders the table and applies the search filter', async () => {
  await signIn('admin')
  const { router } = renderApp('/admin/products')

  expect(await screen.findByRole('cell', { name: 'Keyboard' })).toBeInTheDocument()
  await userEvent.type(screen.getByLabelText('ค้นหาสินค้า'), 'mouse')

  await waitFor(() => expect(router.state.location.search).toBe('?q=mouse'))
  await waitFor(() => expect(screen.queryByRole('cell', { name: 'Keyboard' })).not.toBeInTheDocument())
  expect(screen.getByRole('cell', { name: 'Mouse Pad' })).toBeInTheDocument()
})

it('removes a row after the delete is confirmed', async () => {
  await signIn('admin')
  renderApp('/admin/products')

  await userEvent.click(await screen.findByRole('button', { name: 'ลบ Keyboard' }))
  await userEvent.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'ลบสินค้า' }))

  await waitFor(() => expect(screen.queryByRole('cell', { name: 'Keyboard' })).not.toBeInTheDocument())
  expect(db.products.find((p) => p.name === 'Keyboard')?.isDeleted).toBe(true)
})

it('keeps the row when the delete is cancelled', async () => {
  await signIn('admin')
  renderApp('/admin/products')

  await userEvent.click(await screen.findByRole('button', { name: 'ลบ Keyboard' }))
  await userEvent.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'ยกเลิก' }))

  await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
  expect(screen.getByRole('cell', { name: 'Keyboard' })).toBeInTheDocument()
})

it('links the edit button of a product to its edit page', async () => {
  await signIn('admin')
  renderApp('/admin/products')
  const product = db.products.find((p) => p.name === 'Keyboard')!

  const edit = await screen.findByRole('link', { name: 'แก้ไข Keyboard' })

  expect(edit).toHaveAttribute('href', `/admin/products/${product.id}`)
})
