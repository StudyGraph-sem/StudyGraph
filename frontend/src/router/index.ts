import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import LoginView from '@/views/LoginView.vue'
import RegisterView from '@/views/RegisterView.vue'
import ProfileView from '@/views/ProfileView.vue'

export function safeRedirect(value: unknown): string {
  return typeof value === 'string' && value === '/profile' ? value : '/profile'
}
const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', redirect: '/profile' },
    { path: '/login', component: LoginView, meta: { guestOnly: true } },
    { path: '/register', component: RegisterView, meta: { guestOnly: true } },
    { path: '/profile', component: ProfileView, meta: { requiresAuth: true } },
    { path: '/:pathMatch(.*)*', redirect: '/profile' },
  ],
})
router.beforeEach(async (to) => {
  const auth = useAuthStore()
  await auth.initialize()
  if (to.meta.requiresAuth && !auth.authenticated)
    return { path: '/login', query: { redirect: to.fullPath } }
  if (to.meta.guestOnly && auth.authenticated) return '/profile'
})
export default router
