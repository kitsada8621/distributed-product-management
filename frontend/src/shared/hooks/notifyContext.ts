import { createContext } from 'react'

export type NotifySeverity = 'success' | 'error' | 'info' | 'warning'

export interface Notify {
  (message: string, severity?: NotifySeverity): void
}

export const NotifyContext = createContext<Notify>(() => {})
