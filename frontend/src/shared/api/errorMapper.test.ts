import { expect, it } from 'vitest'
import { toApiError, toNetworkError } from './errorMapper'

function responseOf(status: number, correlationId?: string) {
  const headers = correlationId ? { 'X-Correlation-Id': correlationId } : undefined
  return new Response(null, { status, headers })
}

it('maps a 422 body to field errors', () => {
  const error = toApiError(responseOf(422, 'cid-1'), {
    code: 'VALIDATION_ERROR',
    message: 'Invalid input',
    data: null,
    errors: [{ field: 'email', code: 'INVALID_FORMAT', message: 'Invalid email format' }],
  })

  expect(error.status).toBe(422)
  expect(error.code).toBe('VALIDATION_ERROR')
  expect(error.fieldErrors).toEqual({ email: 'Invalid email format' })
  expect(error.correlationId).toBe('cid-1')
})

it('keeps lockedUntil from a 423 body in details', () => {
  const error = toApiError(responseOf(423), {
    code: 'ACCOUNT_LOCKED',
    message: 'Locked',
    data: { lockedUntil: '2026-10-10T09:00:00Z' },
    errors: null,
    correlationId: 'cid-2',
  })

  expect(error.details).toEqual({ lockedUntil: '2026-10-10T09:00:00Z' })
  expect(error.correlationId).toBe('cid-2')
})

it('returns UNKNOWN when a 500 has no body', () => {
  const error = toApiError(responseOf(500), undefined)

  expect(error.status).toBe(500)
  expect(error.code).toBe('UNKNOWN')
  expect(error.fieldErrors).toEqual({})
})

it('maps a network failure to status 0', () => {
  const error = toNetworkError()

  expect(error.status).toBe(0)
  expect(error.code).toBe('NETWORK_ERROR')
})
