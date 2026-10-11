import { API_BASE_URL, REQUEST_TIMEOUT_MS } from '@/shared/config/env'
import { toApiError, toNetworkError } from './errorMapper'
import type { ApiEnvelope, Page } from './types'

type Method = 'GET' | 'POST' | 'PUT' | 'DELETE'

interface RequestOptions {
  body?: unknown
  query?: Record<string, string | number | undefined>
  headers?: Record<string, string>
  timeoutMs?: number
}

const CSRF_PATHS = ['/api/auth/refresh', '/api/auth/logout']

const NO_REFRESH_PATHS = ['/api/auth/login', '/api/auth/refresh']

let getAccessToken: () => string | null = () => null

export function setAccessTokenGetter(getter: () => string | null) {
  getAccessToken = getter
}

interface AuthRefreshHandlers {
  refresh: () => Promise<unknown>
  onExpired: () => void
}

let authRefresh: AuthRefreshHandlers | null = null

export function setAuthRefreshHandlers(handlers: AuthRefreshHandlers | null) {
  authRefresh = handlers
}

export function readCsrfCookie(): string | null {
  const match = document.cookie.match(/(?:^|;\s*)csrfToken=([^;]*)/)
  return match ? decodeURIComponent(match[1]) : null
}

function buildUrl(path: string, query: RequestOptions['query']): string {
  const url = new URL(path, API_BASE_URL)
  for (const [key, value] of Object.entries(query ?? {})) {
    if (value !== undefined && value !== '') url.searchParams.set(key, String(value))
  }
  return url.toString()
}

function buildHeaders(path: string, options: RequestOptions): Headers {
  const headers = new Headers(options.headers)
  headers.set('Accept', 'application/json')
  if (options.body !== undefined) headers.set('Content-Type', 'application/json')

  const token = getAccessToken()
  if (token) headers.set('Authorization', `Bearer ${token}`)

  if (CSRF_PATHS.includes(path)) {
    const csrf = readCsrfCookie()
    if (csrf) headers.set('X-CSRF-Token', csrf)
  }
  return headers
}

async function parseBody(response: Response): Promise<unknown> {
  const text = await response.text()
  if (!text) return undefined
  try {
    return JSON.parse(text)
  } catch {
    return undefined
  }
}

async function send<T>(
  method: Method,
  path: string,
  options: RequestOptions,
  isRetry = false,
): Promise<ApiEnvelope<T> | undefined> {
  const controller = new AbortController()
  const timer = setTimeout(() => controller.abort(), options.timeoutMs ?? REQUEST_TIMEOUT_MS)

  let response: Response
  let body: unknown
  try {
    response = await fetch(buildUrl(path, options.query), {
      method,
      headers: buildHeaders(path, options),
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
      credentials: 'include',
      signal: controller.signal,
    })
    body = await parseBody(response)
  } catch {
    throw toNetworkError()
  } finally {
    clearTimeout(timer)
  }

  if (response.status === 401 && !isRetry && authRefresh && !NO_REFRESH_PATHS.includes(path)) {
    const refreshed = await refreshOrExpire(authRefresh)
    if (!refreshed) throw toApiError(response, body)
    return send<T>(method, path, options, true)
  }
  if (!response.ok) throw toApiError(response, body)
  return body as ApiEnvelope<T> | undefined
}

async function refreshOrExpire(handlers: AuthRefreshHandlers): Promise<boolean> {
  try {
    await handlers.refresh()
    return true
  } catch {
    handlers.onExpired()
    return false
  }
}

export async function apiRequest<T>(
  method: Method,
  path: string,
  options: RequestOptions = {},
): Promise<T> {
  const envelope = await send<T>(method, path, options)
  return envelope?.data as T
}

export async function apiRequestPage<T>(path: string, options: RequestOptions = {}): Promise<Page<T>> {
  const envelope = await send<T[]>('GET', path, options)
  const items = envelope?.data ?? []
  const meta = envelope?.meta
  return {
    items,
    page: meta?.page ?? 1,
    pageSize: meta?.pageSize ?? items.length,
    total: meta?.total ?? items.length,
  }
}
