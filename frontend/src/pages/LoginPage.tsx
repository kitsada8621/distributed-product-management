import Alert from '@mui/material/Alert'
import Link from '@mui/material/Link'
import Stack from '@mui/material/Stack'
import { Link as RouterLink, useLocation, useSearchParams } from 'react-router'
import { AuthShell, LoginForm } from '@/features/auth'
import { PATHS } from '@/shared/config/paths'

export function LoginPage() {
  const [searchParams] = useSearchParams()
  const location = useLocation()
  const registered = (location.state as { registered?: boolean } | null)?.registered

  return (
    <AuthShell title="เข้าสู่ระบบ">
      <Stack spacing={2}>
        {searchParams.get('expired') ? <Alert severity="info" variant="outlined">
            เซสชันหมดอายุ กรุณาเข้าสู่ระบบอีกครั้ง
          </Alert> : null}
        {registered ? <Alert severity="success" variant="outlined">
            สร้างบัญชีแล้ว เข้าสู่ระบบได้เลย
          </Alert> : null}
        <LoginForm />
        <Stack direction="row" sx={{ justifyContent: 'space-between' }}>
          <Link component={RouterLink} to={PATHS.register}>
            สร้างบัญชีใหม่
          </Link>
          <Link component={RouterLink} to={PATHS.forgotPassword}>
            ลืมรหัสผ่าน?
          </Link>
        </Stack>
      </Stack>
    </AuthShell>
  )
}
