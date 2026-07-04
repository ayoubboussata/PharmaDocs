import { createContext, useContext, useMemo, useState, type ReactNode } from 'react'
import { api, tokenStorage } from '../api/client'
import type { AuthResponse } from '../types'

interface AuthState {
  token: string | null
  email: string | null
  role: string | null
  isAuthenticated: boolean
  isAdmin: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthState | undefined>(undefined)

const EMAIL_KEY = 'pharmadocs.email'
const ROLE_KEY = 'pharmadocs.role'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => tokenStorage.get())
  const [email, setEmail] = useState<string | null>(() => localStorage.getItem(EMAIL_KEY))
  const [role, setRole] = useState<string | null>(() => localStorage.getItem(ROLE_KEY))

  // Registratie is admin-only en gebeurt vanuit de app (zie UsersPage), niet hier.
  async function login(email: string, password: string) {
    const { data } = await api.post<AuthResponse>('/auth/login', { email, password })
    tokenStorage.set(data.token)
    localStorage.setItem(EMAIL_KEY, data.email)
    localStorage.setItem(ROLE_KEY, data.role)
    setToken(data.token)
    setEmail(data.email)
    setRole(data.role)
  }

  const value = useMemo<AuthState>(
    () => ({
      token,
      email,
      role,
      isAuthenticated: Boolean(token),
      isAdmin: role === 'Admin',
      login,
      logout: () => {
        tokenStorage.clear()
        localStorage.removeItem(EMAIL_KEY)
        localStorage.removeItem(ROLE_KEY)
        setToken(null)
        setEmail(null)
        setRole(null)
      },
    }),
    [token, email, role],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth moet binnen een AuthProvider gebruikt worden.')
  return ctx
}
