import { expect, it } from 'vitest'
import { loginSchema, registerSchema, resetPasswordSchema } from './authSchemas'

it('rejects an invalid login email', () => {
  expect(loginSchema.safeParse({ email: 'nope', password: 'x' }).success).toBe(false)
  expect(loginSchema.safeParse({ email: 'a@b.com', password: 'x' }).success).toBe(true)
})

it('requires a name to register', () => {
  const result = registerSchema.safeParse({ email: 'a@b.com', name: ' ', password: 'Orange-Bicycle-77' })

  expect(result.success).toBe(false)
  expect(result.error?.issues.map((issue) => issue.path[0])).toEqual(['name'])
})

it('rejects a short register password in Thai', () => {
  const result = registerSchema.safeParse({ email: 'a@b.com', name: 'Anna', password: 'Zk7!mQp' })

  expect(result.error?.issues[0].path).toEqual(['password'])
  expect(result.error?.issues[0].message).toBe('รหัสผ่านต้องมีอย่างน้อย 8 ตัวอักษร')
})

it('rejects a register password without an uppercase letter', () => {
  const result = registerSchema.safeParse({ email: 'a@b.com', name: 'Anna', password: 'zk7!mqp2' })

  expect(result.error?.issues[0].message).toBe(
    'รหัสผ่านต้องมีตัวพิมพ์ใหญ่ ตัวพิมพ์เล็ก ตัวเลข และสัญลักษณ์อย่างละ 1 ตัวขึ้นไป',
  )
})

it('accepts a valid register password', () => {
  const result = registerSchema.safeParse({ email: 'a@b.com', name: 'Anna', password: 'Orange-Bicycle-77' })

  expect(result.success).toBe(true)
})

it('rejects a register password containing the email local part', () => {
  const result = registerSchema.safeParse({ email: 'somchai@b.com', name: 'Anna', password: 'My-somchai-9!' })

  expect(result.error?.issues[0].path).toEqual(['password'])
  expect(result.error?.issues[0].message).toBe('รหัสผ่านต้องไม่มีอีเมลหรือชื่อของคุณ')
})

it('accepts any non-empty login password', () => {
  expect(loginSchema.safeParse({ email: 'a@b.com', password: 'x' }).success).toBe(true)
})

it('requires the confirmation to match on reset', () => {
  const result = resetPasswordSchema.safeParse({ newPassword: 'Orange-Bicycle-77', confirmPassword: 'other' })

  expect(result.success).toBe(false)
  expect(result.error?.issues[0].path).toEqual(['confirmPassword'])
})

it('rejects a short new password on reset in Thai', () => {
  const result = resetPasswordSchema.safeParse({ newPassword: 'Zk7!mQp', confirmPassword: 'Zk7!mQp' })

  expect(result.error?.issues[0].path).toEqual(['newPassword'])
  expect(result.error?.issues[0].message).toBe('รหัสผ่านต้องมีอย่างน้อย 8 ตัวอักษร')
})

it('rejects a common new password on reset', () => {
  const result = resetPasswordSchema.safeParse({ newPassword: 'Passw0rd!', confirmPassword: 'Passw0rd!' })

  expect(result.error?.issues[0].message).toBe('รหัสผ่านนี้พบได้บ่อยหรือเคยรั่วไหล กรุณาใช้รหัสผ่านอื่น')
})

it('rejects a new password without a digit on reset', () => {
  const result = resetPasswordSchema.safeParse({ newPassword: 'Zk!!mQpx', confirmPassword: 'Zk!!mQpx' })

  expect(result.error?.issues[0].message).toBe(
    'รหัสผ่านต้องมีตัวพิมพ์ใหญ่ ตัวพิมพ์เล็ก ตัวเลข และสัญลักษณ์อย่างละ 1 ตัวขึ้นไป',
  )
})

it('accepts a valid new password with a matching confirmation', () => {
  const result = resetPasswordSchema.safeParse({ newPassword: 'Orange-Bicycle-77', confirmPassword: 'Orange-Bicycle-77' })

  expect(result.success).toBe(true)
})
