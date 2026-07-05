import { NavLink } from 'react-router-dom'
import { LayoutDashboard, FileText, Sparkles, Users, Pill, Sun, Moon, LogOut } from 'lucide-react'
import { useAuth } from '../auth/AuthContext'
import { useTheme } from '../theme/ThemeContext'

const baseNav = [
  { to: '/dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { to: '/documents', label: 'Facturen', icon: FileText },
  { to: '/assistant', label: 'Kennisassistent', icon: Sparkles },
]

export function Sidebar() {
  const { email, isAdmin, logout } = useAuth()
  const { theme, toggle } = useTheme()

  // "Gebruikers" enkel voor admins (registratie is admin-only).
  const nav = isAdmin
    ? [...baseNav, { to: '/users', label: 'Gebruikers', icon: Users }]
    : baseNav

  const initial = email?.[0]?.toUpperCase() ?? '?'

  return (
    <aside className="sticky top-0 flex h-screen w-16 shrink-0 flex-col border-r border-line bg-sidebar lg:w-64">
      {/* Merk */}
      <div className="flex h-16 items-center gap-2.5 px-3 lg:px-5">
        <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-accent text-accent-fg">
          <Pill size={18} />
        </span>
        <span className="hidden text-[15px] font-semibold tracking-tight text-fg lg:block">
          PharmaDocs
        </span>
      </div>

      {/* Navigatie */}
      <nav className="flex-1 space-y-1 px-2 py-3 lg:px-3">
        {nav.map(({ to, label, icon: Icon }) => (
          <NavLink
            key={to}
            to={to}
            className={({ isActive }) =>
              `flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors ${
                isActive
                  ? 'bg-accent-soft text-accent-text'
                  : 'text-muted hover:bg-elevated hover:text-fg'
              }`
            }
          >
            <Icon size={18} className="shrink-0" />
            <span className="hidden lg:block">{label}</span>
          </NavLink>
        ))}
      </nav>

      {/* Onderkant: thema + gebruiker */}
      <div className="space-y-1 border-t border-line px-2 py-3 lg:px-3">
        <button
          onClick={toggle}
          className="flex w-full items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium text-muted transition-colors hover:bg-elevated hover:text-fg"
        >
          {theme === 'dark' ? <Sun size={18} /> : <Moon size={18} />}
          <span className="hidden lg:block">{theme === 'dark' ? 'Licht thema' : 'Donker thema'}</span>
        </button>

        <div className="flex items-center gap-3 rounded-lg px-3 py-2">
          <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-elevated text-sm font-semibold text-fg">
            {initial}
          </span>
          <span className="hidden min-w-0 flex-1 truncate text-sm text-muted lg:block" title={email ?? ''}>
            {email}
          </span>
          <button
            onClick={logout}
            aria-label="Uitloggen"
            title="Uitloggen"
            className="hidden shrink-0 rounded-md p-1.5 text-subtle transition-colors hover:bg-elevated hover:text-danger lg:block"
          >
            <LogOut size={16} />
          </button>
        </div>
      </div>
    </aside>
  )
}
