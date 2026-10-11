import { render } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { ThemeProvider } from '@mui/material/styles'
import { QueryClientProvider } from '@tanstack/react-query'
import { theme } from '@/app/theme'
import { NotifyProvider } from '@/shared/hooks'
import { createQueryWrapper } from './queryWrapper'
import { routes } from '@/app/router/routes'
import { AuthProvider } from '@/features/auth'

export function renderApp(url: string) {
  const router = createMemoryRouter(routes, { initialEntries: [url] })
  const { client } = createQueryWrapper()
  const view = render(
    <QueryClientProvider client={client}>
      <ThemeProvider theme={theme}>
        <NotifyProvider>
          <AuthProvider>
            <RouterProvider router={router} />
          </AuthProvider>
        </NotifyProvider>
      </ThemeProvider>
    </QueryClientProvider>,
  )
  return { router, ...view }
}
