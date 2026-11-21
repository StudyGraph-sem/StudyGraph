<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { safeRedirect } from '@/router'
const auth = useAuthStore()
const router = useRouter()
const route = useRoute()
const email = ref('')
const password = ref('')
const busy = ref(false)
const error = ref('')
async function submit() {
  busy.value = true
  error.value = ''
  try {
    await auth.login({ email: email.value.trim(), password: password.value })
    password.value = ''
    await router.replace(safeRedirect(route.query.redirect))
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : 'Не удалось войти.'
  } finally {
    busy.value = false
  }
}
async function retry() {
  busy.value = true
  try {
    await auth.retrySession()
    if (auth.authenticated) await router.replace('/profile')
  } finally {
    busy.value = false
  }
}
</script>
<template>
  <h1>Вход</h1>
  <p v-if="route.query.registered === '1'" role="status">
    Регистрация завершена. Войдите с вашим email и паролем.
  </p>
  <div v-if="auth.sessionError" role="alert">
    <p>{{ auth.sessionError }}</p>
    <button type="button" :disabled="busy" @click="retry">Повторить восстановление сессии</button>
  </div>
  <form @submit.prevent="submit">
    <fieldset :disabled="busy">
      <label for="login-email">Email</label>
      <input
        id="login-email"
        v-model="email"
        type="email"
        autocomplete="username"
        required
        maxlength="255"
      />
      <label for="login-password">Пароль</label>
      <input
        id="login-password"
        v-model="password"
        type="password"
        autocomplete="current-password"
        required
        maxlength="128"
      />
      <button type="submit">{{ busy ? 'Вход…' : 'Войти' }}</button>
    </fieldset>
    <p v-if="error" role="alert">{{ error }}</p>
  </form>
  <p><RouterLink to="/register">Создать аккаунт</RouterLink></p>
</template>
