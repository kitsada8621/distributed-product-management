import { zodResolver } from '@hookform/resolvers/zod'
import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Stack from '@mui/material/Stack'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { isServiceUnavailable } from '@/shared/api/errorMapper'
import { ApiError } from '@/shared/api/types'
import { FormField, PasswordField, ServiceUnavailableBanner } from '@/shared/ui'
import { register as registerRequest } from '../api/authApi'
import { registerFormSchema, type RegisterFormInput } from '../schemas/authSchemas'

const SERVER_FIELDS = ['email', 'name', 'password'] as const

export function RegisterForm({ onSuccess }: { onSuccess: () => void }) {
  const [formError, setFormError] = useState<ApiError | null>(null)
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormInput>({ resolver: zodResolver(registerFormSchema) })

  function showServerError(error: ApiError) {
    if (error.code === 'EMAIL_EXISTS') {
      setError('email', { message: 'อีเมลนี้ถูกใช้สมัครแล้ว' })
      return
    }
    for (const field of SERVER_FIELDS) {
      const message = error.fieldErrors[field]
      if (message) setError(field, { message })
    }
    if (Object.keys(error.fieldErrors).length === 0) setFormError(error)
  }

  async function submit(values: RegisterFormInput) {
    setFormError(null)
    try {
      await registerRequest({ email: values.email, name: values.name, password: values.password })
      onSuccess()
    } catch (caught) {
      if (caught instanceof ApiError) showServerError(caught)
      else throw caught
    }
  }

  return (
    <form onSubmit={handleSubmit(submit)} noValidate>
      <Stack spacing={1}>
        {formError && isServiceUnavailable(formError) ? <ServiceUnavailableBanner /> : null}
        {formError && !isServiceUnavailable(formError) ? <Alert severity="error" variant="outlined">
            {formError.message}
          </Alert> : null}
        <FormField label="อีเมล" type="email" autoComplete="email" error={errors.email?.message} {...register('email')} />
        <FormField label="ชื่อ" autoComplete="name" error={errors.name?.message} {...register('name')} />
        <PasswordField
          label="รหัสผ่าน"
          autoComplete="new-password"
          error={errors.password?.message}
          {...register('password')}
        />
        <PasswordField
          label="ยืนยันรหัสผ่าน"
          autoComplete="new-password"
          error={errors.confirmPassword?.message}
          {...register('confirmPassword')}
        />
        <Button type="submit" variant="contained" size="large" fullWidth disabled={isSubmitting}>
          สมัครสมาชิก
        </Button>
      </Stack>
    </form>
  )
}
