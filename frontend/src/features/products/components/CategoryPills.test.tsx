import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterAll, afterEach, beforeAll, beforeEach, expect, it, vi } from 'vitest'
import { resetDb } from '@/mocks/db'
import { server } from '@/mocks/server'
import { createQueryWrapper } from '@/test/queryWrapper'
import { CategoryPills } from './CategoryPills'

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
beforeEach(() => resetDb())
afterEach(() => server.resetHandlers())
afterAll(() => server.close())

function renderPills(value: string, onChange = vi.fn()) {
  const { wrapper } = createQueryWrapper()
  render(<CategoryPills value={value} onChange={onChange} />, { wrapper })
  return onChange
}

it('lists ทั้งหมด and every category, with ทั้งหมด pressed when no category is chosen', async () => {
  renderPills('')

  expect(await screen.findByRole('button', { name: 'Electronics' })).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Books' })).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'ทั้งหมด' })).toHaveAttribute('aria-pressed', 'true')
})

it('reports the category id when a category chip is clicked', async () => {
  const onChange = renderPills('')

  await userEvent.click(await screen.findByRole('button', { name: 'Books' }))

  expect(onChange).toHaveBeenCalledWith('c-books')
})

it('marks only the chosen category as pressed', async () => {
  renderPills('c-books')

  expect(await screen.findByRole('button', { name: 'Books' })).toHaveAttribute('aria-pressed', 'true')
  expect(screen.getByRole('button', { name: 'ทั้งหมด' })).toHaveAttribute('aria-pressed', 'false')
})
