import { useState, type FormEvent } from 'react'
import { AxiosError } from 'axios'
import { UserPlus, CheckCircle2 } from 'lucide-react'
import { api } from '../api/client'
import { PageHeader } from '../components/ui/PageHeader'
import { Button } from '../components/ui/Button'
import type { CreatedUserResponse } from '../types'

const inputClass =
  'w-full rounded-lg border border-line bg-canvas px-3 py-2.5 text-sm text-fg placeholder:text-subtle transition-colors focus:border-accent'

export function UsersPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [created, setCreated] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setCreated(null)
    setLoading(true)
    try {
      const { data } = await api.post<CreatedUserResponse>('/auth/register', { email, password })
      setCreated(data.email)
      setEmail('')
      setPassword('')
    } catch (err) {
      const status = err instanceof AxiosError ? err.response?.status : undefined
      if (status === 409) setError('Er bestaat al een account met dit e-mailadres.')
      else if (status === 400) setError('Controleer het e-mailadres en wachtwoord (min. 8 tekens).')
      else if (status === 401 || status === 403) setError('Je hebt geen rechten om accounts aan te maken.')
      else if (status === 429) setError('Te veel aanvragen. Wacht even en probeer het opnieuw.')
      else setError('Er ging iets mis. Probeer het opnieuw.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <>
      <PageHeader
        title="Gebruikers"
        subtitle="Maak accounts aan voor collega's. Registratie staat bewust niet open voor de buitenwereld."
      />

      <div className="mx-auto max-w-md px-6 py-8 lg:px-8">
        <div className="rounded-2xl border border-line bg-surface p-6 shadow-card">
          <div className="mb-5 flex items-center gap-2">
            <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-accent-soft text-accent-text">
              <UserPlus size={18} />
            </span>
            <h2 className="text-sm font-semibold text-fg">Nieuw account</h2>
          </div>

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
                placeholder="collega@apotheek.be"
              />
            </div>

            <div>
              <label htmlFor="password" className="mb-1.5 block text-sm font-medium text-fg">
                Tijdelijk wachtwoord
              </label>
              <input
                id="password"
                type="password"
                required
                minLength={8}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className={inputClass}
                placeholder="min. 8 tekens"
              />
            </div>

            {error && (
              <p className="rounded-lg bg-danger-soft px-3 py-2 text-sm text-danger">{error}</p>
            )}
            {created && (
              <p className="flex items-center gap-2 rounded-lg bg-accent-soft px-3 py-2 text-sm text-accent-text">
                <CheckCircle2 size={16} className="shrink-0" />
                Account aangemaakt voor <span className="font-medium">{created}</span>.
              </p>
            )}

            <Button type="submit" disabled={loading} className="w-full">
              {loading ? 'Bezig…' : 'Account aanmaken'}
            </Button>
          </form>
        </div>
      </div>
    </>
  )
}
