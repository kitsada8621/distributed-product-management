import { render, waitFor } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { expect, it } from 'vitest'
import { AuthContext } from '@/features/auth'
import { routes } from './routes'

it('redirects / to /products', async () => {
  const router = createMemoryRouter(routes, { initialEntries: ['/'] })
  const auth = { status: 'guest' as const, user: null, sessionExpired: false, login: async () => {}, logout: async () => {} }

  render(
    <AuthContext.Provider value={auth}>
      <RouterProvider router={router} />
    </AuthContext.Provider>,
  )

  await waitFor(() => expect(router.state.location.pathname).toBe('/products'))
})
