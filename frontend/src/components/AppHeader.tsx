import { NavLink } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

/** Gedeelde koptekst met navigatie tussen Facturen en Kennisassistent. */
export function AppHeader() {
  const { email, logout } = useAuth()

  const tab = ({ isActive }: { isActive: boolean }) =>
    `rounded-lg px-3 py-1.5 text-sm transition ${
      isActive ? 'bg-slate-800 text-white' : 'text-slate-400 hover:text-slate-200'
    }`

  return (
    <header className="border-b border-slate-800 bg-slate-900">
      <div className="mx-auto flex max-w-5xl items-center justify-between px-6 py-3">
        <div className="flex items-center gap-6">
          <span className="text-lg font-semibold text-white">PharmaDocs</span>
          <nav className="flex items-center gap-1">
            <NavLink to="/documents" className={tab}>
              Facturen
            </NavLink>
            <NavLink to="/assistant" className={tab}>
              Kennisassistent
            </NavLink>
          </nav>
        </div>
        <div className="flex items-center gap-4">
          <span className="hidden text-sm text-slate-400 sm:inline">{email}</span>
          <button
            onClick={logout}
            className="rounded-lg border border-slate-700 px-3 py-1.5 text-sm text-slate-300 transition hover:bg-slate-800"
          >
            Uitloggen
          </button>
        </div>
      </div>
    </header>
  )
}
