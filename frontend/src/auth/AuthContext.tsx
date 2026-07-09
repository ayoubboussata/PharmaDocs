import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { api } from '../api/client'
import type { SessionResponse } from '../types'

interface AuthState {
  email: string | null
  role: string | null
  isAuthenticated: boolean
  isAdmin: boolean
  loading: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthState | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<SessionResponse | null>(null)
  const [loading, setLoading] = useState(true)

  // Bij het opstarten: wie is er ingelogd? Bepaald door de httpOnly-cookie —
  // JavaScript kan die niet lezen, dus we vragen het aan de backend (/auth/me).
  useEffect(() => {
    let active = true
    api
      .get<SessionResponse>('/auth/me')
      .then((res) => active && setUser(res.data))
      .catch(() => active && setUser(null))
      .finally(() => active && setLoading(false))
    return () => {
      active = false
    }
  }, [])

  async function login(email: string, password: string) {
    // De backend zet het token als httpOnly-cookie; wij krijgen enkel wie het is.
    const { data } = await api.post<SessionResponse>('/auth/login', { email, password })
    setUser(data)
  }

  async function logout() {
    try {
      await api.post('/auth/logout') // wist de cookie server-side
    } catch {
      // Cookie is sowieso weg of verlopen — geen probleem.
    }
    setUser(null)
    window.location.assign('/login')
  }

  const value = useMemo<AuthState>(
    () => ({
      email: user?.email ?? null,
      role: user?.role ?? null,
      isAuthenticated: user !== null,
      isAdmin: user?.role === 'Admin',
      loading,
      login,
      logout,
    }),
    [user, loading],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth moet binnen een AuthProvider gebruikt worden.')
  return ctx
}
