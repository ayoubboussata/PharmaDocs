import { createContext, useContext, useMemo, useState, type ReactNode } from 'react'
import { api, tokenStorage } from '../api/client'
import type { AuthResponse } from '../types'

interface AuthState {
  token: string | null
  email: string | null
  isAuthenticated: boolean
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthState | undefined>(undefined)

const EMAIL_KEY = 'pharmadocs.email'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => tokenStorage.get())
  const [email, setEmail] = useState<string | null>(() => localStorage.getItem(EMAIL_KEY))

  async function authenticate(path: 'login' | 'register', email: string, password: string) {
    const { data } = await api.post<AuthResponse>(`/auth/${path}`, { email, password })
    tokenStorage.set(data.token)
    localStorage.setItem(EMAIL_KEY, data.email)
    setToken(data.token)
    setEmail(data.email)
  }

  const value = useMemo<AuthState>(
    () => ({
      token,
      email,
      isAuthenticated: Boolean(token),
      login: (e, p) => authenticate('login', e, p),
      register: (e, p) => authenticate('register', e, p),
      logout: () => {
        tokenStorage.clear()
        localStorage.removeItem(EMAIL_KEY)
        setToken(null)
        setEmail(null)
      },
    }),
    [token, email],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth moet binnen een AuthProvider gebruikt worden.')
  return ctx
}
