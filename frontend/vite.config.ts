import { fileURLToPath, URL } from 'node:url'
import { existsSync, readFileSync } from 'node:fs'
import { Agent } from 'node:https'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'

const certificate = fileURLToPath(
  new URL('./node_modules/.cache/studygraph/localhost.pem', import.meta.url),
)
const key = certificate.replace(/\.pem$/, '.key')
const https =
  existsSync(certificate) && existsSync(key)
    ? { cert: readFileSync(certificate), key: readFileSync(key) }
    : undefined
export default defineConfig({
  plugins: [vue(), vueDevTools()],
  resolve: { alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) } },
  server: {
    host: 'localhost',
    port: 5173,
    strictPort: true,
    https,
    proxy: {
      '/api': {
        target: 'https://localhost:7138',
        changeOrigin: true,
        agent: https ? new Agent({ ca: https.cert }) : undefined,
      },
    },
  },
})
