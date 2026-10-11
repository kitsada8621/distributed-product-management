import { expect, it } from 'vitest'
import { findPasswordIssue } from './passwordPolicy'

const TOO_SHORT = 'รหัสผ่านต้องมีอย่างน้อย 8 ตัวอักษร'
const TOO_LONG = 'รหัสผ่านต้องไม่เกิน 64 ตัวอักษร'
const COMMON = 'รหัสผ่านนี้พบได้บ่อยหรือเคยรั่วไหล กรุณาใช้รหัสผ่านอื่น'
const PATTERN = 'รหัสผ่านเป็นรูปแบบซ้ำหรือเรียงต่อกันที่เดาง่าย'
const COMPOSITION = 'รหัสผ่านต้องมีตัวพิมพ์ใหญ่ ตัวพิมพ์เล็ก ตัวเลข และสัญลักษณ์อย่างละ 1 ตัวขึ้นไป'
const CONTEXT = 'รหัสผ่านต้องไม่มีอีเมลหรือชื่อของคุณ'

it('requires at least 8 characters', () => {
  expect(findPasswordIssue('Zk7!mQp')).toBe(TOO_SHORT)
  expect(findPasswordIssue('Zk7!mQp2')).toBeNull()
})

it('allows up to 64 characters', () => {
  expect(findPasswordIssue(`Aa1!${'x'.repeat(60)}`)).toBeNull()
  expect(findPasswordIssue(`Aa1!${'x'.repeat(61)}`)).toBe(TOO_LONG)
})

it('counts an emoji as one character', () => {
  expect(findPasswordIssue('Zk7!mQ😀')).toBe(TOO_SHORT)
  expect(findPasswordIssue('Zk7!mQ😀2')).toBeNull()
})

it('rejects a common password in any letter case', () => {
  expect(findPasswordIssue('PASSW0RD!')).toBe(COMMON)
})

it('rejects repeated and sequential passwords', () => {
  expect(findPasswordIssue('aaaaaaaaaaaaaaaa')).toBe(PATTERN)
  expect(findPasswordIssue('abcabcabcabcabcabc')).toBe(PATTERN)
  expect(findPasswordIssue('abcdefghijklmnop')).toBe(PATTERN)
  expect(findPasswordIssue('zyxwvutsrqponmlk')).toBe(PATTERN)
})

it('requires uppercase, lowercase, digit and symbol', () => {
  expect(findPasswordIssue('zk7!mqp2')).toBe(COMPOSITION)
  expect(findPasswordIssue('ZK7!MQP2')).toBe(COMPOSITION)
  expect(findPasswordIssue('Zk!!mQpx')).toBe(COMPOSITION)
  expect(findPasswordIssue('Zk7xmQp2')).toBe(COMPOSITION)
})

it('accepts spaces and non-ASCII letters', () => {
  expect(findPasswordIssue('Orange bicycle 77!')).toBeNull()
  expect(findPasswordIssue('Zké7!mQp2')).toBeNull()
})

it('rejects a password containing the email local part', () => {
  expect(findPasswordIssue('My-somchai-9!', { email: 'somchai@example.com' })).toBe(CONTEXT)
})

it('rejects a password containing a word of the name', () => {
  const context = { name: 'Anna Lee' }

  expect(findPasswordIssue('My-Anna-secret-9!', context)).toBe(CONTEXT)
  expect(findPasswordIssue('Ample-Walnut-91!', context)).toBeNull()
})

it('accepts a normal password when there is no context', () => {
  expect(findPasswordIssue('Orange-Bicycle-77')).toBeNull()
})
