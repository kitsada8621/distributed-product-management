import { HttpResponse } from 'msw'
import { API_BASE_URL } from '@/shared/config/env'
import { db, type MockUser, type Role } from './db'

export const apiUrl = (path: string) => `${API_BASE_URL}${path}`

interface Extras {
  status?: number
  message?: string
  meta?: unknown
}

function envelope(init: {
  status: number
  code: string
  message: string
  data: unknown
  meta?: unknown
  errors?: unknown
}) {
  return {
    status: init.status,
    success: init.status < 400,
    code: init.code,
    message: init.message,
    data: init.data,
    meta: init.meta ?? null,
    errors: init.errors ?? null,
    correlationId: crypto.randomUUID(),
    timestamp: new Date().toISOString(),
  }
}

export function ok(data: unknown, extras: Extras = {}) {
  const status = extras.status ?? 200
  const body = envelope({ status, code: 'SUCCESS', message: extras.message ?? 'OK', data, meta: extras.meta })
  return HttpResponse.json(body, {
    status,
    headers: { 'X-Correlation-Id': body.correlationId },
  })
}

export function fail(
  status: number,
  code: string,
  message: string,
  extras: { errors?: unknown; data?: unknown } = {},
) {
  const body = envelope({ status, code, message, data: extras.data ?? null, errors: extras.errors })
  return HttpResponse.json(body, { status, headers: { 'X-Correlation-Id': body.correlationId } })
}

export function validationFail(errors: { field: string; code: string; message: string }[]) {
  return fail(422, 'VALIDATION_ERROR', 'Invalid input', { errors })
}

export const unauthorized = () => fail(401, 'UNAUTHORIZED', 'Authentication required')
export const forbidden = () => fail(403, 'FORBIDDEN', 'You do not have permission')
export const notFound = () => fail(404, 'NOT_FOUND', 'Not found')

export function issueAccessToken(user: MockUser): string {
  const claims = { sub: user.id, role: user.role, email: user.email, ver: db.accessTokenVersion }
  return `mock.${btoa(JSON.stringify(claims))}`
}

function readUser(request: Request): MockUser | null {
  const header = request.headers.get('Authorization')
  if (!header?.startsWith('Bearer mock.')) return null

  try {
    const claims = JSON.parse(atob(header.slice('Bearer mock.'.length)))
    if (claims.ver !== db.accessTokenVersion) return null
    return db.users.find((user) => user.id === claims.sub) ?? null
  } catch {
    return null
  }
}

type Guard = { user: MockUser; error?: undefined } | { user?: undefined; error: Response }

export function authorize(request: Request, role?: Role): Guard {
  const user = readUser(request)
  if (!user) return { error: unauthorized() }
  if (role && user.role !== role) return { error: forbidden() }
  return { user }
}

export function isAdminRequest(request: Request): boolean {
  return readUser(request)?.role === 'admin'
}

export function paginate<T>(rows: T[], url: URL) {
  const page = Math.max(1, Number(url.searchParams.get('page')) || 1)
  const pageSize = Math.min(100, Math.max(1, Number(url.searchParams.get('pageSize')) || 20))
  const start = (page - 1) * pageSize
  const meta = { page, pageSize, total: rows.length, totalPages: Math.ceil(rows.length / pageSize) }
  return { rows: rows.slice(start, start + pageSize), meta }
}
