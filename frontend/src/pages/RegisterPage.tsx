import Link from '@mui/material/Link'
import Stack from '@mui/material/Stack'
import { Link as RouterLink, useNavigate } from 'react-router'
import { AuthShell, RegisterForm } from '@/features/auth'
import { PATHS } from '@/shared/config/paths'

export function RegisterPage() {
  const navigate = useNavigate()

  return (
    <AuthShell title="สมัครสมาชิก">
      <Stack spacing={2}>
        <RegisterForm onSuccess={() => navigate(PATHS.login, { state: { registered: true } })} />
        <Link component={RouterLink} to={PATHS.login}>
          มีบัญชีแล้ว? เข้าสู่ระบบ
        </Link>
      </Stack>
    </AuthShell>
  )
}
