import { apiRequest } from '@/shared/api/httpClient'
import type { User } from '../model/types'

interface AccessTokenResult {
  accessToken: string
}

export function register(input: { email: string; password: string; name: string }) {
  return apiRequest<{ id: string; email: string; role: string }>('POST', '/api/auth/register', { body: input })
}

export function login(input: { email: string; password: string }) {
  return apiRequest<AccessTokenResult>('POST', '/api/auth/login', { body: input })
}

export function refresh() {
  return apiRequest<AccessTokenResult>('POST', '/api/auth/refresh')
}

export function logout() {
  return apiRequest<null>('POST', '/api/auth/logout')
}

export function forgotPassword(email: string) {
  return apiRequest<null>('POST', '/api/auth/forgot-password', { body: { email } })
}

export function resetPassword(input: { token: string; newPassword: string }) {
  return apiRequest<null>('POST', '/api/auth/reset-password', { body: input })
}

export function fetchMe() {
  return apiRequest<User>('GET', '/api/auth/me')
}
