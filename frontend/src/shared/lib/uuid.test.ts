import { expect, it } from 'vitest'
import { uuid } from './uuid'

it('returns a v4 uuid and a different value each call', () => {
  const first = uuid()

  expect(first).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/)
  expect(uuid()).not.toBe(first)
})
