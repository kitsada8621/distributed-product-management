import { zodResolver } from '@hookform/resolvers/zod'
import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Link from '@mui/material/Link'
import Stack from '@mui/material/Stack'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link as RouterLink } from 'react-router'
import { isServiceUnavailable } from '@/shared/api/errorMapper'
import { ApiError } from '@/shared/api/types'
import { PATHS } from '@/shared/config/paths'
import { PasswordField, ServiceUnavailableBanner } from '@/shared/ui'
import { resetPassword } from '../api/authApi'
import { resetPasswordSchema, type ResetPasswordInput } from '../schemas/authSchemas'

interface ResetPasswordFormProps {
  token: string
  onSuccess: () => void
}

export function ResetPasswordForm({ token, onSuccess }: ResetPasswordFormProps) {
  const [error, setError] = useState<ApiError | null>(null)
  const {
    register,
    handleSubmit,
    setError: setFieldError,
    formState: { errors, isSubmitting },
  } = useForm<ResetPasswordInput>({ resolver: zodResolver(resetPasswordSchema) })

  async function submit(values: ResetPasswordInput) {
    setError(null)
    try {
      await resetPassword({ token, newPassword: values.newPassword })
      onSuccess()
    } catch (caught) {
      if (!(caught instanceof ApiError)) throw caught
      if (caught.fieldErrors.newPassword) setFieldError('newPassword', { message: caught.fieldErrors.newPassword })
      else setError(caught)
    }
  }

  return (
    <form onSubmit={handleSubmit(submit)} noValidate>
      <Stack spacing={1}>
        {error && isServiceUnavailable(error) ? <ServiceUnavailableBanner /> : null}
        {error?.code === 'RESET_TOKEN_INVALID' ? (
          <Alert severity="error" variant="outlined">
            ลิงก์ไม่ถูกต้องหรือหมดอายุ{' '}
            <Link component={RouterLink} to={PATHS.forgotPassword}>
              ขอลิงก์ใหม่
            </Link>
          </Alert>
        ) : null}
        {error && error.code !== 'RESET_TOKEN_INVALID' && !isServiceUnavailable(error) ? (
          <Alert severity="error" variant="outlined">
            {error.message}
          </Alert>
        ) : null}
        <PasswordField
          label="รหัสผ่านใหม่"
          autoComplete="new-password"
          error={errors.newPassword?.message}
          {...register('newPassword')}
        />
        <PasswordField
          label="ยืนยันรหัสผ่านใหม่"
          autoComplete="new-password"
          error={errors.confirmPassword?.message}
          {...register('confirmPassword')}
        />
        <Button type="submit" variant="contained" fullWidth disabled={isSubmitting}>
          บันทึกรหัสผ่านใหม่
        </Button>
      </Stack>
    </form>
  )
}
