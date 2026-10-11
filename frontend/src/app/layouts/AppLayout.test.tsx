import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterAll, afterEach, beforeAll, beforeEach, expect, it } from 'vitest'
import { server } from '@/mocks/server'
import { renderApp } from '@/test/renderApp'
import { resetTestState, signIn } from '@/test/session'

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
beforeEach(() => resetTestState())
afterEach(() => server.resetHandlers())
afterAll(() => server.close())

it('opens the admin menu drawer with the three admin links', async () => {
  await signIn('admin')
  renderApp('/admin/categories')

  await userEvent.click(await screen.findByRole('button', { name: 'เปิดเมนูผู้ดูแลระบบ' }))

  const menu = within(await screen.findByRole('navigation', { name: 'เมนูผู้ดูแลระบบ' }))
  expect(menu.getByRole('link', { name: 'สินค้า' })).toHaveAttribute('href', '/admin/products')
  expect(menu.getByRole('link', { name: 'หมวดหมู่' })).toHaveAttribute('href', '/admin/categories')
  expect(menu.getByRole('link', { name: 'คำสั่งซื้อ' })).toHaveAttribute('href', '/admin/orders')
})

it('has no admin menu button outside the admin area', async () => {
  await signIn('admin')
  renderApp('/products')

  await screen.findByRole('link', { name: 'ผู้ดูแลระบบ' })

  expect(screen.queryByRole('button', { name: 'เปิดเมนูผู้ดูแลระบบ' })).not.toBeInTheDocument()
})
