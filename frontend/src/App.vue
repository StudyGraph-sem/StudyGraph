<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'
const auth = useAuthStore()
</script>

<template>
  <header>
    <strong>StudyGraph</strong>
    <nav aria-label="Основная навигация">
      <template v-if="auth.authenticated"><RouterLink to="/profile">Профиль</RouterLink></template>
      <template v-else>
        <RouterLink to="/login">Вход</RouterLink>
        <RouterLink to="/register">Регистрация</RouterLink>
      </template>
    </nav>
  </header>
  <main>
    <p v-if="!auth.initialized" role="status">�������������� ������</p>
    <RouterView />
  </main>
</template>
