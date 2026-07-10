import { Navigate } from 'react-router-dom'
import type { ReactNode } from 'react'
import { useAuth } from '../auth/AuthContext'

/** Laat enkel de operator (SystemAdmin) door; andere ingelogde gebruikers gaan terug naar /documents. */
export function SystemAdminRoute({ children }: { children: ReactNode }) {
  const { isSystemAdmin } = useAuth()
  return isSystemAdmin ? <>{children}</> : <Navigate to="/documents" replace />
}
