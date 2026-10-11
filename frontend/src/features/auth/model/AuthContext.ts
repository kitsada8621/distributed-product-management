import { createContext } from 'react'
import type { User } from './types'

export type AuthStatus = 'loading' | 'authed' | 'guest'

export interface AuthContextValue {
  status: AuthStatus
  user: User | null
  sessionExpired: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | null>(null)
