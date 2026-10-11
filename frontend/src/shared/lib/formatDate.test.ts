import { expect, it } from 'vitest'
import { formatDate } from './formatDate'

it('converts an ISO UTC time to the requested time zone', () => {
  expect(formatDate('2026-10-10T08:30:00Z', 'UTC')).toBe('10 Oct 2026, 08:30')
  expect(formatDate('2026-10-10T08:30:00Z', 'Asia/Bangkok')).toBe('10 Oct 2026, 15:30')
})
