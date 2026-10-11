import { Navigate, Outlet, useSearchParams } from 'react-router'
import { useAuth } from '@/features/auth'
import { LoadingState } from '@/shared/ui'
import { safeReturnTo } from '@/shared/config/paths'

export function GuestOnly() {
  const { status } = useAuth()
  const [searchParams] = useSearchParams()

  if (status === 'loading') return <LoadingState label="Checking session" />
  if (status === 'authed') return <Navigate to={safeReturnTo(searchParams.get('returnTo'))} replace />
  return <Outlet />
}
