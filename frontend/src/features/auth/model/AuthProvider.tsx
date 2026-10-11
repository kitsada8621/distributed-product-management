import { useQueryClient } from '@tanstack/react-query'
import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { readCsrfCookie, setAuthRefreshHandlers } from '@/shared/api/httpClient'
import { ORDER_DRAFT_STORAGE_KEY } from '@/shared/config/storageKeys'
import { emitSessionCleared } from '@/shared/lib/sessionEvents'
import { fetchMe, login as loginRequest, logout as logoutRequest } from '../api/authApi'
import { AuthContext, type AuthContextValue, type AuthStatus } from './AuthContext'
import { refreshAccessToken } from './refreshSession'
import { clearAccessToken, setAccessToken } from './session'
import type { User } from './types'

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const [status, setStatus] = useState<AuthStatus>('loading')
  const [user, setUser] = useState<User | null>(null)
  const [sessionExpired, setSessionExpired] = useState(false)

  useEffect(() => {
    restoreSession()
      .then((restored) => {
        setUser(restored)
        setStatus('authed')
      })
      .catch(() => setStatus('guest'))
  }, [])

  useEffect(() => {
    setAuthRefreshHandlers({
      refresh: refreshAccessToken,
      onExpired: () => {
        clearAccessToken()
        queryClient.clear()
        emitSessionCleared()
        setUser(null)
        setSessionExpired(true)
        setStatus('guest')
      },
    })
    return () => setAuthRefreshHandlers(null)
  }, [queryClient])

  const login = useCallback(async (email: string, password: string) => {
    const { accessToken } = await loginRequest({ email, password })
    setAccessToken(accessToken)
    setUser(await fetchMe())
    setSessionExpired(false)
    setStatus('authed')
  }, [])

  const logout = useCallback(async () => {
    await logoutRequest().catch(() => undefined)
    clearAccessToken()
    queryClient.clear()
    emitSessionCleared()
    sessionStorage.removeItem(ORDER_DRAFT_STORAGE_KEY)
    setUser(null)
    setStatus('guest')
  }, [queryClient])

  const value = useMemo<AuthContextValue>(
    () => ({ status, user, sessionExpired, login, logout }),
    [status, user, sessionExpired, login, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

async function restoreSession(): Promise<User> {
  if (!readCsrfCookie()) throw new Error('no session')
  await refreshAccessToken()
  return fetchMe()
}
