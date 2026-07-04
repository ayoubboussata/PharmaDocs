import { Navigate } from 'react-router-dom'
import type { ReactNode } from 'react'
import { useAuth } from '../auth/AuthContext'

/** Laat enkel admins door; andere ingelogde gebruikers gaan terug naar /documents. */
export function AdminRoute({ children }: { children: ReactNode }) {
  const { isAdmin } = useAuth()
  return isAdmin ? <>{children}</> : <Navigate to="/documents" replace />
}
