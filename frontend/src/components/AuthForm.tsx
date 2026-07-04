import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { AxiosError } from 'axios'
import { Pill, Sun, Moon } from 'lucide-react'
import { Button } from './ui/Button'
import { useTheme } from '../theme/ThemeContext'

interface AuthFormProps {
  title: string
  submitLabel: string
  onSubmit: (email: string, password: string) => Promise<void>
  footer?: { text: string; linkText: string; to: string }
  note?: string
}

const inputClass =
  'w-full rounded-lg border border-line bg-canvas px-3 py-2.5 text-sm text-fg placeholder:text-subtle transition-colors focus:border-accent'

export function AuthForm({ title, submitLabel, onSubmit, footer, note }: AuthFormProps) {
  const { theme, toggle } = useTheme()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setLoading(true)
    try {
      await onSubmit(email, password)
    } catch (err) {
      const status = err instanceof AxiosError ? err.response?.status : undefined
      if (status === 401) setError('Ongeldige inloggegevens.')
      else if (status === 409) setError('Er bestaat al een account met dit e-mailadres.')
      else if (status === 400) setError('Controleer je e-mailadres en wachtwoord (min. 8 tekens).')
      else if (status === 429) setError('Te veel pogingen. Wacht even en probeer het opnieuw.')
      else setError('Er ging iets mis. Probeer het opnieuw.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="relative flex min-h-screen items-center justify-center bg-canvas px-4 text-fg">
      <button
        onClick={toggle}
        aria-label="Thema wisselen"
        className="absolute right-5 top-5 rounded-lg p-2 text-muted transition-colors hover:bg-elevated hover:text-fg"
      >
        {theme === 'dark' ? <Sun size={18} /> : <Moon size={18} />}
      </button>

      <div className="w-full max-w-sm">
        <div className="mb-8 flex flex-col items-center text-center">
          <span className="mb-4 flex h-12 w-12 items-center justify-center rounded-2xl bg-accent text-accent-fg shadow-card">
            <Pill size={24} />
          </span>
          <h1 className="text-xl font-semibold tracking-tight text-fg">PharmaDocs</h1>
          <p className="mt-1 text-sm text-muted">{title}</p>
        </div>

        <div className="rounded-2xl border border-line bg-surface p-7 shadow-card">
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label htmlFor="email" className="mb-1.5 block text-sm font-medium text-fg">
                E-mail
              </label>
              <input
                id="email"
                type="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className={inputClass}
                placeholder="jij@apotheek.be"
              />
            </div>

            <div>
              <label htmlFor="password" className="mb-1.5 block text-sm font-medium text-fg">
                Wachtwoord
              </label>
              <input
                id="password"
                type="password"
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className={inputClass}
                placeholder="••••••••"
              />
            </div>

            {error && (
              <p className="rounded-lg bg-danger-soft px-3 py-2 text-sm text-danger">{error}</p>
            )}

            <Button type="submit" disabled={loading} className="w-full">
              {loading ? 'Bezig…' : submitLabel}
            </Button>
          </form>
        </div>

        {footer && (
          <p className="mt-6 text-center text-sm text-muted">
            {footer.text}{' '}
            <Link to={footer.to} className="font-medium text-accent-text hover:underline">
              {footer.linkText}
            </Link>
          </p>
        )}
        {note && <p className="mt-6 text-center text-sm text-subtle">{note}</p>}
      </div>
    </div>
  )
}
