import { AppProviders } from '@/app/providers/AppProviders'
import { AppRouter } from '@/app/router/router'
import { AuthProvider } from '@/features/auth'

export default function App() {
  return (
    <AppProviders>
      <AuthProvider>
        <AppRouter />
      </AuthProvider>
    </AppProviders>
  )
}
