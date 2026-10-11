import Alert from '@mui/material/Alert'
import Link from '@mui/material/Link'
import { useEffect, useState } from 'react'
import { Link as RouterLink, useLocation, useNavigate, useSearchParams } from 'react-router'
import { AuthShell, ResetPasswordForm } from '@/features/auth'
import { PATHS } from '@/shared/config/paths'

export function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const location = useLocation()
  const navigate = useNavigate()
  const [token] = useState(() => searchParams.get('token'))

  useEffect(() => {
    window.history.replaceState(window.history.state, '', location.pathname)
  }, [location.pathname])

  return (
    <AuthShell title="ตั้งรหัสผ่านใหม่">
      {token ? (
        <ResetPasswordForm token={token} onSuccess={() => navigate(PATHS.login)} />
      ) : (
        <Alert severity="error" variant="outlined">
          ลิงก์ไม่ถูกต้องหรือหมดอายุ{' '}
          <Link component={RouterLink} to={PATHS.forgotPassword}>
            ขอลิงก์ใหม่
          </Link>
        </Alert>
      )}
    </AuthShell>
  )
}
