import { Navigate, Outlet, useLocation } from 'react-router'
import { useAuth } from '@/features/auth'
import { LoadingState } from '@/shared/ui'
import { PATHS, loginPath } from '@/shared/config/paths'

interface RequireAuthProps {
  role?: 'admin'
}

export function RequireAuth({ role }: RequireAuthProps) {
  const { status, user, sessionExpired } = useAuth()
  const location = useLocation()

  if (status === 'loading') return <LoadingState label="Checking session" />
  if (status === 'guest') {
    return <Navigate to={loginPath(location.pathname + location.search, sessionExpired)} replace />
  }
  if (role && user?.role !== role) return <Navigate to={PATHS.forbidden} replace />
  return <Outlet />
}
