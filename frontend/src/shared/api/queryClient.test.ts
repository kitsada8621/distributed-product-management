import { expect, it } from 'vitest'
import { createQueryClient, shouldRetryQuery } from './queryClient'
import { ApiError } from './types'

const apiError = (status: number) => new ApiError({ status, code: 'X', message: '' })

it('does not retry 4xx errors', () => {
  expect(shouldRetryQuery(0, apiError(404))).toBe(false)
  expect(shouldRetryQuery(0, apiError(422))).toBe(false)
})

it('retries 5xx and network errors up to 2 times', () => {
  expect(shouldRetryQuery(0, apiError(500))).toBe(true)
  expect(shouldRetryQuery(1, apiError(0))).toBe(true)
  expect(shouldRetryQuery(2, apiError(500))).toBe(false)
})

it('never retries mutations', () => {
  expect(createQueryClient().getDefaultOptions().mutations?.retry).toBe(false)
})
