import Link from '@mui/material/Link'
import Stack from '@mui/material/Stack'
import { Link as RouterLink } from 'react-router'
import { AuthShell, ForgotPasswordForm } from '@/features/auth'
import { PATHS } from '@/shared/config/paths'

export function ForgotPasswordPage() {
  return (
    <AuthShell title="ลืมรหัสผ่าน">
      <Stack spacing={2}>
        <ForgotPasswordForm />
        <Link component={RouterLink} to={PATHS.login}>
          กลับไปหน้าเข้าสู่ระบบ
        </Link>
      </Stack>
    </AuthShell>
  )
}
