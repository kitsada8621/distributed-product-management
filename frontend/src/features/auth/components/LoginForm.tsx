import { zodResolver } from '@hookform/resolvers/zod'
import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Stack from '@mui/material/Stack'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { ApiError } from '@/shared/api/types'
import { isServiceUnavailable } from '@/shared/api/errorMapper'
import { FormField, PasswordField, ServiceUnavailableBanner } from '@/shared/ui'
import { useAuth } from '../model/useAuth'
import { loginSchema, type LoginInput } from '../schemas/authSchemas'
import { LockedNotice } from './LockedNotice'

function ErrorNotice({ error }: { error: ApiError }) {
  if (isServiceUnavailable(error)) return <ServiceUnavailableBanner />
  if (error.code === 'ACCOUNT_LOCKED') {
    return <LockedNotice lockedUntil={String(error.details?.lockedUntil ?? '')} />
  }
  if (error.code === 'INVALID_CREDENTIALS') return <Alert severity="error" variant="outlined">อีเมลหรือรหัสผ่านไม่ถูกต้อง</Alert>
  if (error.code === 'RATE_LIMITED') return <Alert severity="warning" variant="outlined">พยายามหลายครั้งเกินไป กรุณารอสักครู่แล้วลองใหม่</Alert>
  return <Alert severity="error" variant="outlined">
      {error.message || 'เข้าสู่ระบบไม่สำเร็จ'}
    </Alert>
}

export function LoginForm() {
  const { login } = useAuth()
  const [error, setError] = useState<ApiError | null>(null)
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginInput>({ resolver: zodResolver(loginSchema) })

  async function submit(values: LoginInput) {
    setError(null)
    try {
      await login(values.email, values.password)
    } catch (caught) {
      setError(caught instanceof ApiError ? caught : new ApiError({ status: 0, code: 'UNKNOWN', message: '' }))
    }
  }

  return (
    <form onSubmit={handleSubmit(submit)} noValidate>
      <Stack spacing={1}>
        {error ? <ErrorNotice error={error} /> : null}
        <FormField label="อีเมล" type="email" autoComplete="email" error={errors.email?.message} {...register('email')} />
        <PasswordField
          label="รหัสผ่าน"
          autoComplete="current-password"
          error={errors.password?.message}
          {...register('password')}
        />
        <Button type="submit" variant="contained" size="large" fullWidth disabled={isSubmitting}>
          เข้าสู่ระบบ
        </Button>
      </Stack>
    </form>
  )
}
