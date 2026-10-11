import { http } from 'msw'
import { db, saveSession, type MockUser } from '../db'
import { apiUrl, authorize, fail, issueAccessToken, ok, validationFail } from '../http'

const MAX_FAILURES = 5
const LOCK_MINUTES = 15
const MIN_PASSWORD_LENGTH = 8
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

interface Credentials {
  email?: string
  password?: string
  name?: string
}

function startSession(user: MockUser) {
  const session = { userId: user.id, refreshToken: crypto.randomUUID(), csrfToken: crypto.randomUUID() }
  saveSession(session)
  document.cookie = `csrfToken=${session.csrfToken}; path=/`
  return session
}

function hasValidCsrf(request: Request): boolean {
  const header = request.headers.get('X-CSRF-Token')
  return Boolean(db.session) && header === db.session?.csrfToken
}

const csrfInvalid = () => fail(403, 'CSRF_INVALID', 'CSRF check failed')

function recordLoginFailure(email: string) {
  const entry = db.loginFailures.get(email) ?? { count: 0, lockedUntil: null }
  entry.count += 1
  if (entry.count >= MAX_FAILURES) {
    entry.lockedUntil = new Date(Date.now() + LOCK_MINUTES * 60_000).toISOString()
  }
  db.loginFailures.set(email, entry)
}

export const authHandlers = [
  http.post(apiUrl('/api/auth/register'), async ({ request }) => {
    const body = (await request.json()) as Credentials
    const errors = []
    if (!body.email || !EMAIL_PATTERN.test(body.email)) {
      errors.push({ field: 'email', code: 'INVALID_FORMAT', message: 'Invalid email format' })
    }
    if (!body.password || body.password.length < MIN_PASSWORD_LENGTH) {
      errors.push({ field: 'password', code: 'TOO_SHORT', message: 'Password must be at least 8 characters' })
    }
    if (!body.name?.trim()) {
      errors.push({ field: 'name', code: 'REQUIRED', message: 'Name is required' })
    }
    if (errors.length > 0) return validationFail(errors)

    if (db.users.some((user) => user.email === body.email)) {
      return fail(409, 'EMAIL_EXISTS', 'This email is already registered')
    }
    const user: MockUser = {
      id: crypto.randomUUID(),
      email: body.email!,
      name: body.name!.trim(),
      role: 'user',
      password: body.password!,
    }
    db.users.push(user)
    return ok({ id: user.id, email: user.email, role: user.role }, { status: 201, message: 'Registered' })
  }),

  http.post(apiUrl('/api/auth/login'), async ({ request }) => {
    const body = (await request.json()) as Credentials
    if (!body.email || !body.password) {
      return validationFail([{ field: body.email ? 'password' : 'email', code: 'REQUIRED', message: 'Required' }])
    }

    const failures = db.loginFailures.get(body.email)
    if (failures?.lockedUntil && new Date(failures.lockedUntil) > new Date()) {
      return fail(423, 'ACCOUNT_LOCKED', 'Account is locked', { data: { lockedUntil: failures.lockedUntil } })
    }

    const user = db.users.find((candidate) => candidate.email === body.email)
    if (!user || user.password !== body.password) {
      recordLoginFailure(body.email)
      return fail(401, 'INVALID_CREDENTIALS', 'Invalid email or password')
    }

    db.loginFailures.delete(body.email)
    startSession(user)
    return ok({ accessToken: issueAccessToken(user) }, { message: 'Login successful' })
  }),

  http.post(apiUrl('/api/auth/refresh'), ({ request }) => {
    if (!hasValidCsrf(request)) return csrfInvalid()

    const user = db.users.find((candidate) => candidate.id === db.session?.userId)
    if (!user) return fail(401, 'REFRESH_INVALID', 'Session expired')

    startSession(user)
    return ok({ accessToken: issueAccessToken(user) })
  }),

  http.post(apiUrl('/api/auth/logout'), ({ request }) => {
    const guard = authorize(request)
    if (guard.error) return guard.error
    if (!hasValidCsrf(request)) return csrfInvalid()

    saveSession(null)
    document.cookie = 'csrfToken=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/'
    return ok(null, { message: 'Logged out' })
  }),

  http.post(apiUrl('/api/auth/forgot-password'), async ({ request }) => {
    const body = (await request.json()) as Credentials
    if (!body.email || !EMAIL_PATTERN.test(body.email)) {
      return validationFail([{ field: 'email', code: 'INVALID_FORMAT', message: 'Invalid email format' }])
    }

    if (db.users.some((user) => user.email === body.email)) {
      const token = crypto.randomUUID()
      db.resetTokens.set(token, body.email)
      console.info(`[mock] reset link: /reset-password?token=${token}`)
    }
    return ok(null, { status: 202, message: 'If the email exists, a reset link was sent' })
  }),

  http.post(apiUrl('/api/auth/reset-password'), async ({ request }) => {
    const body = (await request.json()) as { token?: string; newPassword?: string }
    if (!body.newPassword || body.newPassword.length < MIN_PASSWORD_LENGTH) {
      return validationFail([{ field: 'newPassword', code: 'TOO_SHORT', message: 'Password must be at least 8 characters' }])
    }

    const email = body.token ? db.resetTokens.get(body.token) : undefined
    const user = db.users.find((candidate) => candidate.email === email)
    if (!body.token || !user) return fail(400, 'RESET_TOKEN_INVALID', 'The reset link is invalid or expired')

    user.password = body.newPassword
    db.resetTokens.delete(body.token)
    saveSession(null)
    return ok(null, { message: 'Password changed' })
  }),

  http.get(apiUrl('/api/auth/me'), ({ request }) => {
    const guard = authorize(request)
    if (guard.error) return guard.error

    const { id, email, name, role } = guard.user
    return ok({ id, email, name, role })
  }),
]
