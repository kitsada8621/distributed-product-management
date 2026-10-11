import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router'
import { afterEach, expect, it } from 'vitest'
import { AuthContext, type AuthContextValue } from '@/features/auth'
import { useOrderDraftStore } from '../model/orderDraftStore'
import { AddToOrderButton } from './AddToOrderButton'

const product = { id: 'p1', name: 'Keyboard', price: 100, inStock: true }
const user = { id: 'u', email: 'a@b.c', name: 'A', role: 'user' as const }

afterEach(() => useOrderDraftStore.getState().clear())

function Where() {
  const location = useLocation()
  return <div>at:{location.pathname + location.search}</div>
}

function renderButton(auth: Partial<AuthContextValue>, inStock = true) {
  const value: AuthContextValue = {
    status: 'guest',
    user: null,
    sessionExpired: false,
    login: async () => {},
    logout: async () => {},
    ...auth,
  }
  render(
    <AuthContext.Provider value={value}>
      <MemoryRouter initialEntries={['/products?page=2']}>
        <Routes>
          <Route path="/products" element={<AddToOrderButton product={{ ...product, inStock }} />} />
          <Route path="*" element={<Where />} />
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  )
}

it('sends a guest to login and keeps the page as returnTo', async () => {
  renderButton({ status: 'guest' })

  await userEvent.click(screen.getByRole('button', { name: 'เพิ่มในรายการ' }))

  expect(screen.getByText('at:/login?returnTo=%2Fproducts%3Fpage%3D2')).toBeInTheDocument()
  expect(useOrderDraftStore.getState().items).toEqual([])
})

it('adds to the draft and opens the order page for a signed-in user', async () => {
  renderButton({ status: 'authed', user })

  await userEvent.click(screen.getByRole('button', { name: 'เพิ่มในรายการ' }))

  expect(screen.getByText('at:/orders/new')).toBeInTheDocument()
  expect(useOrderDraftStore.getState().items).toEqual([
    { productId: 'p1', name: 'Keyboard', unitPrice: 100, quantity: 1 },
  ])
})

it('is disabled when the product is out of stock', () => {
  renderButton({ status: 'authed', user }, false)

  expect(screen.getByRole('button', { name: 'สินค้าหมด' })).toBeDisabled()
})
