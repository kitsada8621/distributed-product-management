import CssBaseline from '@mui/material/CssBaseline'
import { ThemeProvider } from '@mui/material/styles'
import { QueryClientProvider } from '@tanstack/react-query'
import { useState, type ReactNode } from 'react'
import { createQueryClient } from '@/shared/api/queryClient'
import { NotifyProvider } from '@/shared/hooks/NotifyProvider'
import { theme } from '../theme'

export function AppProviders({ children }: { children: ReactNode }) {
  const [queryClient] = useState(createQueryClient)

  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <NotifyProvider>{children}</NotifyProvider>
      </ThemeProvider>
    </QueryClientProvider>
  )
}
