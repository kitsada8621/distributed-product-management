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

it('sends a plain user to the 403 page', async () => {
  await signIn('user')
  const { router } = renderApp('/admin/categories')

  await screen.findByRole('heading', { name: 'ไม่มีสิทธิ์เข้าถึง' })

  expect(router.state.location.pathname).toBe('/403')
})

it('creates a category', async () => {
  await signIn('admin')
  renderApp('/admin/categories')

  await userEvent.click(await screen.findByRole('button', { name: 'สร้างหมวดหมู่' }))
  await userEvent.type(await screen.findByLabelText('ชื่อหมวดหมู่'), 'Toys')
  await userEvent.click(screen.getByRole('button', { name: 'บันทึก' }))

  expect(await screen.findByText('Toys')).toBeInTheDocument()
})

it('requires a name', async () => {
  await signIn('admin')
  renderApp('/admin/categories')

  await userEvent.click(await screen.findByRole('button', { name: 'สร้างหมวดหมู่' }))
  await userEvent.click(await screen.findByRole('button', { name: 'บันทึก' }))

  expect(await screen.findByText('Name is required')).toBeInTheDocument()
})

it('edits a category name', async () => {
  await signIn('admin')
  renderApp('/admin/categories')

  await userEvent.click(await screen.findByRole('button', { name: 'แก้ไข Books' }))
  const input = await screen.findByLabelText('ชื่อหมวดหมู่')
  await userEvent.clear(input)
  await userEvent.type(input, 'Magazines')
  await userEvent.click(screen.getByRole('button', { name: 'บันทึก' }))

  expect(await screen.findByText('Magazines')).toBeInTheDocument()
  expect(screen.queryByText('Books')).not.toBeInTheDocument()
})

it('deletes an empty category', async () => {
  await signIn('admin')
  renderApp('/admin/categories')

  await userEvent.click(await screen.findByRole('button', { name: 'ลบ Empty category' }))
  await userEvent.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'ลบ' }))

  await screen.findByText('ลบหมวดหมู่แล้ว')
  expect(screen.queryByText('Empty category')).not.toBeInTheDocument()
})

it('deletes a category that still has products', async () => {
  await signIn('admin')
  renderApp('/admin/categories')

  await userEvent.click(await screen.findByRole('button', { name: 'ลบ Books' }))
  await userEvent.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'ลบ' }))

  await screen.findByText('ลบหมวดหมู่แล้ว')
  expect(screen.queryByText('Books')).not.toBeInTheDocument()
})
