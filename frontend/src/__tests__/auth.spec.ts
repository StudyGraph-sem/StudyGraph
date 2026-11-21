import { beforeEach, afterEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useAuthStore } from '@/stores/auth'
import { authApi } from '@/api/auth'
import { ApiError } from '@/api/http'

const user = {
  id: '1',
  displayName: 'Тест',
  email: 'test@example.com',
  role: 'User',
  createdAt: new Date().toISOString(),
}
const session = {
  user,
  accessToken: 'access',
  expiresAt: new Date(Date.now() + 900000).toISOString(),
}
function reply(body: unknown, status = 200) {
  return new Response(status === 204 ? null : JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}
let fetchMock = vi.fn()
beforeEach(() => {
  setActivePinia(createPinia())
  localStorage.clear()
  sessionStorage.clear()
  fetchMock = vi.fn()
  vi.stubGlobal('fetch', fetchMock)
})
afterEach(() => vi.unstubAllGlobals())

describe('Session', () => {
  it('restores once for concurrent initialization', async () => {
    fetchMock.mockResolvedValue(reply(session))
    const auth = useAuthStore()
    await Promise.all([auth.initialize(), auth.initialize()])
    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(auth.authenticated).toBe(true)
  })
  it('treats missing cookie as a guest', async () => {
    fetchMock.mockResolvedValue(reply({}, 401))
    const auth = useAuthStore()
    await auth.initialize()
    expect(auth.initialized).toBe(true)
    expect(auth.authenticated).toBe(false)
    expect(auth.sessionError).toBe('')
  })
  it('reports a temporary restore failure and permits retry', async () => {
    fetchMock.mockRejectedValueOnce(new TypeError('offline')).mockResolvedValueOnce(reply(session))
    const auth = useAuthStore()
    await auth.initialize()
    expect(auth.sessionError).not.toBe('')
    await auth.retrySession()
    expect(auth.authenticated).toBe(true)
    expect(auth.sessionError).toBe('')
  })
  it('sends credentials and CSRF header without persisting secrets', async () => {
    fetchMock.mockResolvedValue(reply(session))
    const auth = useAuthStore()
    await auth.login({ email: user.email, password: 'Example123!' })
    const options = fetchMock.mock.calls[0]![1] as RequestInit
    expect(options.credentials).toBe('include')
    expect(new Headers(options.headers).get('X-StudyGraph-CSRF')).toBe('1')
    expect(localStorage.length).toBe(0)
    expect(sessionStorage.length).toBe(0)
  })
  it('shares a single refresh across concurrent requests', async () => {
    fetchMock.mockResolvedValue(reply(session))
    const auth = useAuthStore()
    await Promise.all([auth.refresh(), auth.refresh(), auth.refresh()])
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })
  it('refreshes on 401 then retries the protected request', async () => {
    fetchMock
      .mockResolvedValueOnce(reply(session))
      .mockResolvedValueOnce(reply({}, 401))
      .mockResolvedValueOnce(reply({ ...session, accessToken: 'new-access' }))
      .mockResolvedValueOnce(reply(user))
    const auth = useAuthStore()
    await auth.login({ email: user.email, password: 'Example123!' })
    await auth.loadProfile()
    expect(fetchMock.mock.calls.map((call) => call[0])).toEqual([
      '/api/auth/login',
      '/api/auth/me',
      '/api/auth/refresh',
      '/api/auth/me',
    ])
    const options = fetchMock.mock.calls[3]![1] as RequestInit
    expect(new Headers(options.headers).get('Authorization')).toBe('Bearer new-access')
  })
  it('clears an expired session when refresh returns 401', async () => {
    fetchMock.mockResolvedValueOnce(reply(session)).mockResolvedValueOnce(reply({}, 401))
    const auth = useAuthStore()
    await auth.login({ email: user.email, password: 'Example123!' })
    await expect(auth.refresh()).rejects.toBeInstanceOf(ApiError)
    expect(auth.authenticated).toBe(false)
  })
  it('logs out only after the server revokes the session', async () => {
    fetchMock
      .mockResolvedValueOnce(reply(session))
      .mockRejectedValueOnce(new Error('offline'))
      .mockResolvedValueOnce(reply(null, 204))
    const auth = useAuthStore()
    await auth.login({ email: user.email, password: 'Example123!' })
    await expect(auth.logout()).rejects.toBeInstanceOf(ApiError)
    expect(auth.authenticated).toBe(true)
    await auth.logout()
    expect(auth.authenticated).toBe(false)
    expect(auth.user).toBeNull()
  })
  it('shows a useful duplicate email error', async () => {
    fetchMock.mockResolvedValue(reply({}, 409))
    await expect(
      authApi.register({ email: user.email, password: 'Example123!', displayName: 'Тест' }),
    ).rejects.toThrow('Пользователь с таким email уже зарегистрирован.')
  })
})

describe('Protected routes', () => {
  it('redirects a guest away from the profile', async () => {
    const { default: router } = await import('@/router')
    const auth = useAuthStore()
    auth.initialized = true
    await router.push('/profile')
    expect(router.currentRoute.value.path).toBe('/login')
    expect(router.currentRoute.value.query.redirect).toBe('/profile')
  })
  it('redirects a signed-in user away from registration', async () => {
    const { default: router } = await import('@/router')
    const auth = useAuthStore()
    fetchMock.mockResolvedValue(reply(session))
    await auth.login({ email: user.email, password: 'Example123!' })
    await router.push('/register')
    expect(router.currentRoute.value.path).toBe('/profile')
  })
})
