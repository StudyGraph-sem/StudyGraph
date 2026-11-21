<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { authApi } from '@/api/auth'
const router = useRouter()
const email = ref('')
const displayName = ref('')
const password = ref('')
const confirmation = ref('')
const busy = ref(false)
const error = ref('')
async function submit() {
  error.value = ''
  if (!displayName.value.trim()) {
    error.value = 'Введите имя.'
    return
  }
  if (password.value !== confirmation.value) {
    error.value = 'Пароли не совпадают.'
    return
  }
  busy.value = true
  try {
    await authApi.register({
      email: email.value.trim(),
      displayName: displayName.value.trim(),
      password: password.value,
    })
    password.value = ''
    confirmation.value = ''
    await router.replace({ path: '/login', query: { registered: '1' } })
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : 'Не удалось зарегистрироваться.'
  } finally {
    busy.value = false
  }
}
</script>
<template>
  <h1>Регистрация</h1>
  <form @submit.prevent="submit">
    <fieldset :disabled="busy">
      <label for="register-name">Имя</label>
      <input
        id="register-name"
        v-model="displayName"
        autocomplete="name"
        required
        maxlength="100"
      />
      <label for="register-email">Email</label>
      <input
        id="register-email"
        v-model="email"
        type="email"
        autocomplete="username"
        required
        maxlength="255"
      />
      <label for="register-password">Пароль (от 8 до 128 символов)</label>
      <input
        id="register-password"
        v-model="password"
        type="password"
        autocomplete="new-password"
        required
        minlength="8"
        maxlength="128"
      />
      <label for="register-confirmation">Повторите пароль</label>
      <input
        id="register-confirmation"
        v-model="confirmation"
        type="password"
        autocomplete="new-password"
        required
        minlength="8"
        maxlength="128"
      />
      <button type="submit">{{ busy ? 'Регистрация…' : 'Зарегистрироваться' }}</button>
    </fieldset>
    <p v-if="error" role="alert">{{ error }}</p>
  </form>
  <p><RouterLink to="/login">Уже есть аккаунт? Войти</RouterLink></p>
</template>
