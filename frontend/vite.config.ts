import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    proxy: {
      // Alles onder /api gaat naar de .NET-backend — zo vermijden we CORS in dev.
      '/api': {
        target: 'http://localhost:5035',
        changeOrigin: true,
      },
    },
  },
})
