import { authApi, clearAccessToken, setAccessToken } from '@/features/auth'
import { resetDb } from '@/mocks/db'

const ACCOUNTS = {
  user: { email: 'user@example.com', password: 'Passw0rd!' },
  admin: { email: 'admin@example.com', password: 'Admin123!' },
}

export async function signIn(who: keyof typeof ACCOUNTS) {
  const { accessToken } = await authApi.login(ACCOUNTS[who])
  setAccessToken(accessToken)
}

export function resetTestState() {
  resetDb()
  clearAccessToken()
  sessionStorage.clear()
  document.cookie = 'csrfToken=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/'
}
