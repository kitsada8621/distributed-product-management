import { ThemeProvider } from '@mui/material/styles'
import { QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import type { ReactElement } from 'react'
import { MemoryRouter } from 'react-router'
import { theme } from '@/app/theme'
import { NotifyProvider } from '@/shared/hooks/NotifyProvider'
import { createQueryClient } from '@/shared/api/queryClient'

export function renderWithProviders(ui: ReactElement, route = '/') {
  const queryClient = createQueryClient()
  queryClient.setDefaultOptions({
    queries: { retry: false },
    mutations: { retry: false },
  })

  return render(
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={theme}>
        <NotifyProvider>
          <MemoryRouter initialEntries={[route]}>{ui}</MemoryRouter>
        </NotifyProvider>
      </ThemeProvider>
    </QueryClientProvider>,
  )
}
