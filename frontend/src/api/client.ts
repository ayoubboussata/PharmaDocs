import axios from 'axios'

// Basis-URL /api → via de Vite/nginx-proxy naar de .NET-backend (same-origin, geen CORS).
// withCredentials: de httpOnly auth-cookie (L1) gaat automatisch mee; er staat géén
// token in JavaScript (niet leesbaar → niet steelbaar via XSS).
export const api = axios.create({ baseURL: '/api', withCredentials: true })

// Bij een 401 op een gewone call (sessie verlopen/ongeldig): terug naar login.
// De auth-endpoints zelf (/auth/me, /auth/login) mogen een 401 geven zonder
// redirect-lus — die handelt de AuthContext af.
api.interceptors.response.use(
  (response) => response,
  (error) => {
    const url: string = error.config?.url ?? ''
    const isAuthCall = url.includes('/auth/')
    if (error.response?.status === 401 && !isAuthCall && window.location.pathname !== '/login') {
      window.location.assign('/login')
    }
    return Promise.reject(error)
  },
)
