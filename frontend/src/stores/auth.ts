import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { authApi } from '@/api/auth'
import { ApiError, request } from '@/api/http'
import type { AuthResponse, LoginRequest, User } from '@/types/auth'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null)
  const accessToken = ref<string | null>(null)
  const expiresAt = ref<string | null>(null)
  const initialized = ref(false)
  const sessionError = ref('')
  const authenticated = computed(() => user.value !== null && accessToken.value !== null)
  let refreshing: Promise<void> | null = null
  let restoring: Promise<void> | null = null

  function apply(data: AuthResponse) {
    user.value = data.user
    accessToken.value = data.accessToken
    expiresAt.value = data.expiresAt
    sessionError.value = ''
  }
  function clear() {
    user.value = null
    accessToken.value = null
    expiresAt.value = null
  }
  async function refresh() {
    if (!refreshing) {
      refreshing = (async () => {
        const renew = async () => apply(await authApi.refresh())
        if (typeof navigator !== 'undefined' && navigator.locks) {
          await navigator.locks.request('studygraph-session', renew)
        } else {
          await renew()
        }
      })()
        .catch((error: unknown) => {
          if (error instanceof ApiError && error.status === 401) clear()
          throw error
        })
        .finally(() => {
          refreshing = null
        })
    }
    return refreshing
  }
  async function initialize() {
    if (initialized.value) return
    if (!restoring) {
      restoring = (async () => {
        try {
          await refresh()
        } catch (error) {
          if (!(error instanceof ApiError && error.status === 401)) {
            sessionError.value =
              error instanceof Error ? error.message : 'Не удалось восстановить сессию.'
          }
        } finally {
          initialized.value = true
          restoring = null
        }
      })()
    }
    return restoring
  }
  async function login(data: LoginRequest) {
    if (refreshing) await refreshing.catch(() => {})
    apply(await authApi.login(data))
    initialized.value = true
  }
  async function logout() {
    if (refreshing) await refreshing.catch(() => {})
    await authApi.logout()
    clear()
    sessionError.value = ''
  }
  async function authorizedRequest<T>(path: string, options: RequestInit = {}): Promise<T> {
    if (!accessToken.value || Date.parse(expiresAt.value ?? '') <= Date.now() + 5000)
      await refresh()
    const send = () => {
      const headers = new Headers(options.headers)
      headers.set('Authorization', `Bearer ${accessToken.value}`)
      return request<T>(path, { ...options, headers })
    }
    const token = accessToken.value
    try {
      return await send()
    } catch (error) {
      if (!(error instanceof ApiError && error.status === 401)) throw error
      if (token === accessToken.value) await refresh()
      try {
        return await send()
      } catch (retryError) {
        if (retryError instanceof ApiError && retryError.status === 401) clear()
        throw retryError
      }
    }
  }
  async function loadProfile() {
    try {
      user.value = await authorizedRequest<User>('/auth/me')
    } catch (error) {
      if (error instanceof ApiError && error.status === 404) clear()
      throw error
    }
  }
  async function retrySession() {
    initialized.value = false
    sessionError.value = ''
    await initialize()
  }
  return {
    user,
    expiresAt,
    authenticated,
    initialized,
    sessionError,
    initialize,
    retrySession,
    login,
    logout,
    refresh,
    authorizedRequest,
    loadProfile,
  }
})
