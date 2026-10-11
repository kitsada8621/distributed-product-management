import { zodResolver } from '@hookform/resolvers/zod'
import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Stack from '@mui/material/Stack'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { isServiceUnavailable } from '@/shared/api/errorMapper'
import { ApiError } from '@/shared/api/types'
import { FormField, ServiceUnavailableBanner } from '@/shared/ui'
import { forgotPassword } from '../api/authApi'
import { forgotPasswordSchema, type ForgotPasswordInput } from '../schemas/authSchemas'

const SENT_MESSAGE = 'หากอีเมลนี้มีในระบบ จะได้รับลิงก์ภายในไม่กี่นาที'

export function ForgotPasswordForm() {
  const [sent, setSent] = useState(false)
  const [error, setError] = useState<ApiError | null>(null)
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ForgotPasswordInput>({ resolver: zodResolver(forgotPasswordSchema) })

  async function submit(values: ForgotPasswordInput) {
    setError(null)
    try {
      await forgotPassword(values.email)
      setSent(true)
    } catch (caught) {
      if (caught instanceof ApiError) setError(caught)
      else throw caught
    }
  }

  if (sent) return <Alert severity="success" variant="outlined">
        {SENT_MESSAGE}
      </Alert>

  return (
    <form onSubmit={handleSubmit(submit)} noValidate>
      <Stack spacing={1}>
        {error && isServiceUnavailable(error) ? <ServiceUnavailableBanner /> : null}
        {error && !isServiceUnavailable(error) ? <Alert severity="error" variant="outlined">
            {error.message}
          </Alert> : null}
        <FormField label="อีเมล" type="email" autoComplete="email" error={errors.email?.message} {...register('email')} />
        <Button type="submit" variant="contained" fullWidth disabled={isSubmitting}>
          ส่งลิงก์ตั้งรหัสผ่านใหม่
        </Button>
      </Stack>
    </form>
  )
}
