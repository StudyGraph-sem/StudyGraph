import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import App from '../App.vue'

describe('Application navigation', () => {
  it('shows guest navigation and routed content', async () => {
    const router = createRouter({
      history: createMemoryHistory(),
      routes: [{ path: '/', component: { template: '<p>Страница входа</p>' } }],
    })
    await router.push('/')
    const wrapper = mount(App, { global: { plugins: [createPinia(), router] } })
    expect(wrapper.text()).toContain('StudyGraph')
    expect(wrapper.text()).toContain('Регистрация')
    expect(wrapper.text()).toContain('Страница входа')
  })
})
