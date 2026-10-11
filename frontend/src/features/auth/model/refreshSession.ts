import { refresh } from '../api/authApi'
import { setAccessToken } from './session'

let inFlight: Promise<string> | null = null

export function refreshAccessToken(): Promise<string> {
  if (!inFlight) {
    inFlight = refresh()
      .then(({ accessToken }) => {
        setAccessToken(accessToken)
        return accessToken
      })
      .finally(() => {
        inFlight = null
      })
  }
  return inFlight
}
