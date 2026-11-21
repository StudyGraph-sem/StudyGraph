import { mkdirSync } from 'node:fs'
import { execFileSync, spawn } from 'node:child_process'
import { fileURLToPath } from 'node:url'
const root = fileURLToPath(new URL('../', import.meta.url))
const directory = fileURLToPath(new URL('../node_modules/.cache/studygraph/', import.meta.url))
mkdirSync(directory, { recursive: true })
try {
  execFileSync(
    'dotnet',
    [
      'dev-certs',
      'https',
      '--export-path',
      `${directory}/localhost.pem`,
      '--format',
      'Pem',
      '--no-password',
    ],
    { cwd: root, stdio: 'inherit' },
  )
} catch {
  console.error(
    'Не удалось экспортировать сертификат. Выполните dotnet dev-certs https --trust и повторите запуск.',
  )
  process.exit(1)
}
const child = spawn(process.execPath, ['node_modules/vite/bin/vite.js', ...process.argv.slice(2)], {
  cwd: root,
  stdio: 'inherit',
})
child.on('exit', (code) => process.exit(code ?? 1))
for (const signal of ['SIGINT', 'SIGTERM']) process.on(signal, () => child.kill(signal))
