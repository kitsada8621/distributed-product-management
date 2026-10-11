import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { afterAll, afterEach, beforeAll, beforeEach, expect, it } from 'vitest'
import { resetDb } from '@/mocks/db'
import { apiUrl } from '@/mocks/http'
import { server } from '@/mocks/server'
import { renderApp } from '@/test/renderApp'

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
beforeEach(() => resetDb())
afterEach(() => server.resetHandlers())
afterAll(() => server.close())

it('lists products with category and stock badge for a guest', async () => {
  renderApp('/products')

  expect(await screen.findByRole('link', { name: 'Atlas' })).toBeInTheDocument()
  expect(screen.getAllByText(/มีของ|สินค้าหมด/).length).toBeGreaterThan(0)
})

it('shows the second page of results', async () => {
  const { router } = renderApp('/products?page=2')

  await screen.findByRole('link', { name: 'Tablet' })

  expect(router.state.location.search).toBe('?page=2')
  expect(screen.queryByRole('link', { name: 'Atlas' })).not.toBeInTheDocument()
})

it('search resets the page to 1 and writes q to the URL', async () => {
  const { router } = renderApp('/products?page=2')
  await screen.findByRole('link', { name: 'Tablet' })

  await userEvent.type(screen.getByLabelText('ค้นหาสินค้า'), 'mouse')

  await waitFor(() => expect(router.state.location.search).toBe('?q=mouse'))
  expect(await screen.findByRole('link', { name: 'Mouse Pad' })).toBeInTheDocument()
  expect(screen.queryByRole('link', { name: 'Atlas' })).not.toBeInTheDocument()
})

it('filters by category', async () => {
  const { router } = renderApp('/products')
  await screen.findByRole('link', { name: 'Atlas' })

  await userEvent.click(screen.getByRole('combobox', { name: 'หมวดหมู่' }))
  await userEvent.click(await screen.findByRole('option', { name: 'Books' }))

  await waitFor(() => expect(router.state.location.search).toBe('?categoryId=c-books'))
  await waitFor(() => expect(screen.queryByRole('link', { name: 'Keyboard' })).not.toBeInTheDocument())
  expect(screen.getByRole('link', { name: 'Novel' })).toBeInTheDocument()
})

it('shows an empty state when nothing matches', async () => {
  renderApp('/products?q=zzzz')

  expect(await screen.findByText('ไม่พบสินค้าที่ค้นหา')).toBeInTheDocument()
})

it('shows an error state with retry for a 4xx failure', async () => {
  server.use(
    http.get(apiUrl('/api/catalog/products'), () =>
      HttpResponse.json({ status: 400, code: 'BAD_REQUEST', message: 'Bad request', data: null }, { status: 400 }),
    ),
  )
  renderApp('/products')

  const alert = await screen.findByRole('alert')

  expect(within(alert).getByText('Bad request')).toBeInTheDocument()
  expect(within(alert).getByRole('button', { name: 'ลองใหม่' })).toBeInTheDocument()
})

it('shows the service banner when the service is down', async () => {
  server.use(http.get(apiUrl('/api/catalog/products'), () => HttpResponse.error()))
  renderApp('/products')

  expect(await screen.findByText(/ระบบไม่พร้อมให้บริการชั่วคราว/)).toBeInTheDocument()
})

it('shows the skeleton grid while products are loading', async () => {
  server.use(http.get(apiUrl('/api/catalog/products'), () => new Promise(() => {})))
  renderApp('/products')

  expect(await screen.findByRole('status', { name: 'กำลังโหลดสินค้า' })).toBeInTheDocument()
})

it('writes categoryId to the URL when a category pill is clicked', async () => {
  const { router } = renderApp('/products')
  await screen.findByRole('link', { name: 'Atlas' })

  await userEvent.click(screen.getByRole('button', { name: 'Books' }))

  await waitFor(() => expect(router.state.location.search).toBe('?categoryId=c-books'))
})
