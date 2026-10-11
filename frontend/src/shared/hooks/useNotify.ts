import { useContext } from 'react'
import { NotifyContext } from './notifyContext'

export function useNotify() {
  return useContext(NotifyContext)
}
