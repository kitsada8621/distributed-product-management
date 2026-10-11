import { z } from 'zod'
import { findPasswordIssue } from './passwordPolicy'

const email = z.email('Invalid email format')

export const loginSchema = z.object({
  email,
  password: z.string().min(1, 'Password is required'),
})

const registerFields = z.object({
  email,
  name: z.string().trim().min(1, 'Name is required'),
  password: z.string(),
})

function checkRegisterPassword(value: z.infer<typeof registerFields>, context: z.RefinementCtx) {
  const issue = findPasswordIssue(value.password, { email: value.email, name: value.name })
  if (issue) context.addIssue({ code: 'custom', path: ['password'], message: issue })
}

export const registerSchema = registerFields.superRefine(checkRegisterPassword)

export const registerFormSchema = registerFields
  .extend({ confirmPassword: z.string() })
  .superRefine(checkRegisterPassword)
  .refine((value) => value.password === value.confirmPassword, {
    path: ['confirmPassword'],
    message: 'Passwords do not match',
  })

function checkNewPassword(value: string, context: z.RefinementCtx) {
  const issue = findPasswordIssue(value)
  if (issue) context.addIssue({ code: 'custom', message: issue })
}

export const forgotPasswordSchema = z.object({ email })

export const resetPasswordSchema = z
  .object({
    newPassword: z.string().superRefine(checkNewPassword),
    confirmPassword: z.string(),
  })
  .refine((value) => value.newPassword === value.confirmPassword, {
    path: ['confirmPassword'],
    message: 'Passwords do not match',
  })

export type LoginInput = z.infer<typeof loginSchema>
export type RegisterInput = z.infer<typeof registerSchema>
export type RegisterFormInput = z.infer<typeof registerFormSchema>
export type ForgotPasswordInput = z.infer<typeof forgotPasswordSchema>
export type ResetPasswordInput = z.infer<typeof resetPasswordSchema>
