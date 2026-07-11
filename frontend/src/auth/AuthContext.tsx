import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { api } from '../api/client'
import type { SessionResponse } from '../types'

interface AuthState {
  email: string | null
  role: string | null
  organization: string | null
  isAuthenticated: boolean
  isAdmin: boolean
  isSystemAdmin: boolean
  loading: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthState | undefined>(undefined)

// --- Per-tenant branding (MT12): pas de accentkleur van de apotheek toe ---
function hexToRgb(hex: string): { r: number; g: number; b: number } | null {
  const match = /^#?([0-9a-f]{6})$/i.exec(hex.trim())
  if (!match) return null
  const n = parseInt(match[1], 16)
  return { r: (n >> 16) & 255, g: (n >> 8) & 255, b: n & 255 }
}

// Zet (of wist) de accent-tokens op de root, zodat de UI de kleur van de apotheek draagt.
function applyBrandColor(color: string | null) {
  const root = document.documentElement
  const vars = ['--accent', '--accent-hover', '--accent-soft', '--accent-text']
  const rgb = color ? hexToRgb(color) : null
  if (!rgb) {
    vars.forEach((v) => root.style.removeProperty(v))
    return
  }
  root.style.setProperty('--accent', color!)
  root.style.setProperty('--accent-hover', color!)
  root.style.setProperty('--accent-text', color!)
  root.style.setProperty('--accent-soft', `rgba(${rgb.r}, ${rgb.g}, ${rgb.b}, 0.14)`)
}

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

  // Branding: pas de accentkleur van de apotheek toe (of wis ze bij uitloggen).
  useEffect(() => {
    applyBrandColor(user?.organizationColor ?? null)
  }, [user])

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
      organization: user?.organization ?? null,
      isAuthenticated: user !== null,
      isAdmin: user?.role === 'Admin',
      isSystemAdmin: user?.role === 'SystemAdmin',
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
