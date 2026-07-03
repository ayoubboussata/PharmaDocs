import axios from 'axios'

const TOKEN_KEY = 'pharmadocs.token'

export const tokenStorage = {
  get: () => localStorage.getItem(TOKEN_KEY),
  set: (token: string) => localStorage.setItem(TOKEN_KEY, token),
  clear: () => localStorage.removeItem(TOKEN_KEY),
}

// Basis-URL is /api → via de Vite-proxy naar de .NET-backend (geen CORS in dev).
export const api = axios.create({ baseURL: '/api' })

// Hang bij elke request automatisch het bearer-token aan.
api.interceptors.request.use((config) => {
  const token = tokenStorage.get()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// Bij een 401 (token verlopen/ongeldig): opruimen en terug naar login.
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401 && tokenStorage.get()) {
      tokenStorage.clear()
      if (window.location.pathname !== '/login') {
        window.location.assign('/login')
      }
    }
    return Promise.reject(error)
  },
)
