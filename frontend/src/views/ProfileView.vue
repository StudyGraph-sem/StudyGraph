<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
const auth = useAuthStore()
const router = useRouter()
const error = ref('')
const busy = ref(false)
watch(
  () => auth.authenticated,
  (value) => {
    if (!value) void router.replace('/login')
  },
)
async function run(action: () => Promise<void>) {
  busy.value = true
  error.value = ''
  try {
    await action()
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : 'Не удалось выполнить запрос.'
  } finally {
    busy.value = false
  }
}
onMounted(() => {
  void run(() => auth.loadProfile())
})
async function logout() {
  await auth.logout()
  await router.replace('/login')
}
async function renew() {
  await auth.refresh()
  await auth.loadProfile()
}
</script>
<template>
  <h1>Личный кабинет</h1>
  <dl v-if="auth.user">
    <dt>Имя</dt>
    <dd>{{ auth.user.displayName }}</dd>
    <dt>Email</dt>
    <dd>{{ auth.user.email }}</dd>
    <dt>Роль</dt>
    <dd>{{ auth.user.role }}</dd>
    <dt>Дата регистрации</dt>
    <dd>{{ new Date(auth.user.createdAt).toLocaleString('ru-RU') }}</dd>
  </dl>
  <p v-if="auth.expiresAt">
    Access token действует до {{ new Date(auth.expiresAt).toLocaleTimeString('ru-RU') }}.
  </p>
  <button :disabled="busy" @click="run(() => auth.loadProfile())">Обновить профиль</button>
  <button :disabled="busy" @click="run(renew)">Обновить сессию</button>
  <button :disabled="busy" @click="run(logout)">Выйти</button>
  <p v-if="busy" role="status">Выполняется запрос…</p>
  <p v-if="error" role="alert">{{ error }}</p>
</template>
