import { ApiError, type FieldErrorBody } from './types'

export const NETWORK_ERROR = 'NETWORK_ERROR'
export const UNKNOWN_ERROR = 'UNKNOWN'

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

function toFieldErrors(errors: unknown): Record<string, string> {
  const result: Record<string, string> = {}
  if (!Array.isArray(errors)) return result

  for (const item of errors as FieldErrorBody[]) {
    result[item.field] = item.message
  }
  return result
}

export function toApiError(response: Response, body: unknown): ApiError {
  const headerCorrelationId = response.headers.get('X-Correlation-Id')

  if (!isRecord(body) || typeof body.code !== 'string') {
    return new ApiError({
      status: response.status,
      code: UNKNOWN_ERROR,
      message: 'Unexpected response from the server',
      correlationId: headerCorrelationId,
    })
  }

  const bodyCorrelationId = typeof body.correlationId === 'string' ? body.correlationId : null
  return new ApiError({
    status: response.status,
    code: body.code,
    message: typeof body.message === 'string' ? body.message : '',
    fieldErrors: toFieldErrors(body.errors),
    details: isRecord(body.data) ? body.data : null,
    correlationId: headerCorrelationId ?? bodyCorrelationId,
  })
}

export function toNetworkError(): ApiError {
  return new ApiError({
    status: 0,
    code: NETWORK_ERROR,
    message: 'Cannot reach the server',
  })
}

export function isServiceUnavailable(error: ApiError): boolean {
  return error.status === 0 || error.status >= 500
}

export function isNotFound(error: unknown): boolean {
  return error instanceof ApiError && error.status === 404
}
