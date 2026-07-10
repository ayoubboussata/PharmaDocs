import { useEffect, useState, type FormEvent } from 'react'
import { AxiosError } from 'axios'
import { Building2, Plus, CheckCircle2, Trash2 } from 'lucide-react'
import { api } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { PageHeader } from '../components/ui/PageHeader'
import { Button } from '../components/ui/Button'
import { Spinner } from '../components/ui/Spinner'
import type { Organization } from '../types'

const inputClass =
  'w-full rounded-lg border border-line bg-canvas px-3 py-2.5 text-sm text-fg placeholder:text-subtle transition-colors focus:border-accent'

export function OrganizationsPage() {
  const { organization: ownOrganization } = useAuth()
  const [name, setName] = useState('')
  const [adminEmail, setAdminEmail] = useState('')
  const [adminPassword, setAdminPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [created, setCreated] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  const [organizations, setOrganizations] = useState<Organization[]>([])
  const [listLoading, setListLoading] = useState(true)
  const [deletingId, setDeletingId] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    api
      .get<Organization[]>('/organizations')
      .then((res) => active && setOrganizations(res.data))
      .catch(() => active && setError('Kon de organisaties niet laden.'))
      .finally(() => active && setListLoading(false))
    return () => {
      active = false
    }
  }, [])

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setCreated(null)
    setSaving(true)
    try {
      const { data } = await api.post<Organization>('/organizations', {
        name,
        adminEmail,
        adminPassword,
      })
      setOrganizations((prev) => [data, ...prev])
      setCreated(data.name)
      setName('')
      setAdminEmail('')
      setAdminPassword('')
    } catch (err) {
      const status = err instanceof AxiosError ? err.response?.status : undefined
      if (status === 409) setError('Er bestaat al een organisatie of account met deze gegevens.')
      else if (status === 400) setError('Controleer de naam, het e-mailadres en het wachtwoord (min. 8 tekens).')
      else if (status === 401 || status === 403) setError('Enkel een operator mag organisaties aanmaken.')
      else if (status === 429) setError('Te veel aanvragen. Wacht even en probeer het opnieuw.')
      else setError('Er ging iets mis. Probeer het opnieuw.')
    } finally {
      setSaving(false)
    }
  }

  async function handleDelete(org: Organization) {
    const confirmed = window.confirm(
      `"${org.name}" en ál haar data (facturen, kennisbank, gebruikers) definitief verwijderen? Dit kan niet ongedaan gemaakt worden.`,
    )
    if (!confirmed) return

    setError(null)
    setDeletingId(org.id)
    try {
      await api.delete(`/organizations/${org.id}`)
      setOrganizations((prev) => prev.filter((o) => o.id !== org.id))
    } catch (err) {
      const status = err instanceof AxiosError ? err.response?.status : undefined
      if (status === 400) setError('Deze organisatie kan niet verwijderd worden.')
      else if (status === 401 || status === 403) setError('Enkel een operator mag organisaties verwijderen.')
      else setError('Verwijderen mislukt. Probeer het opnieuw.')
    } finally {
      setDeletingId(null)
    }
  }

  return (
    <>
      <PageHeader
        title="Organisaties"
        subtitle="Onboarding van apotheken. Elke organisatie is een aparte tenant met strikt gescheiden data; je maakt ze aan met hun eerste beheerder."
      />

      <div className="mx-auto grid max-w-5xl gap-6 px-6 py-8 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.1fr)] lg:px-8">
        {/* Nieuwe organisatie */}
        <div className="h-fit rounded-2xl border border-line bg-surface p-6 shadow-card">
          <div className="mb-5 flex items-center gap-2">
            <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-accent-soft text-accent-text">
              <Plus size={18} />
            </span>
            <h2 className="text-sm font-semibold text-fg">Nieuwe apotheek</h2>
          </div>

          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label htmlFor="name" className="mb-1.5 block text-sm font-medium text-fg">
                Naam apotheek
              </label>
              <input
                id="name"
                type="text"
                required
                maxLength={200}
                value={name}
                onChange={(e) => setName(e.target.value)}
                className={inputClass}
                placeholder="Apotheek Zonnebloem"
              />
            </div>

            <div>
              <label htmlFor="adminEmail" className="mb-1.5 block text-sm font-medium text-fg">
                E-mail beheerder
              </label>
              <input
                id="adminEmail"
                type="email"
                required
                value={adminEmail}
                onChange={(e) => setAdminEmail(e.target.value)}
                className={inputClass}
                placeholder="admin@zonnebloem.be"
              />
            </div>

            <div>
              <label htmlFor="adminPassword" className="mb-1.5 block text-sm font-medium text-fg">
                Tijdelijk wachtwoord
              </label>
              <input
                id="adminPassword"
                type="password"
                required
                minLength={8}
                value={adminPassword}
                onChange={(e) => setAdminPassword(e.target.value)}
                className={inputClass}
                placeholder="min. 8 tekens"
              />
            </div>

            {error && <p className="rounded-lg bg-danger-soft px-3 py-2 text-sm text-danger">{error}</p>}
            {created && (
              <p className="flex items-center gap-2 rounded-lg bg-accent-soft px-3 py-2 text-sm text-accent-text">
                <CheckCircle2 size={16} className="shrink-0" />
                Apotheek <span className="font-medium">{created}</span> aangemaakt.
              </p>
            )}

            <Button type="submit" disabled={saving} className="w-full">
              {saving ? 'Bezig…' : 'Apotheek aanmaken'}
            </Button>
          </form>
        </div>

        {/* Lijst */}
        <div className="rounded-2xl border border-line bg-surface p-6 shadow-card">
          <h2 className="mb-4 text-sm font-semibold text-fg">
            Aangesloten apotheken{organizations.length > 0 && ` (${organizations.length})`}
          </h2>

          {listLoading ? (
            <div className="flex justify-center py-8">
              <Spinner />
            </div>
          ) : organizations.length === 0 ? (
            <p className="py-8 text-center text-sm text-muted">Nog geen organisaties aangemaakt.</p>
          ) : (
            <ul className="divide-y divide-line">
              {organizations.map((org) => {
                // De eigen apotheek van de operator kan niet verwijderd worden.
                const isOwn = org.name === ownOrganization
                return (
                  <li key={org.id} className="flex items-center gap-3 py-3">
                    <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-elevated text-muted">
                      <Building2 size={16} />
                    </span>
                    <span className="min-w-0 flex-1">
                      <span className="block truncate text-sm font-medium text-fg">{org.name}</span>
                      <span className="block truncate text-xs text-subtle">{org.slug}</span>
                    </span>
                    <span className="shrink-0 text-xs text-subtle">
                      {new Date(org.createdAt).toLocaleDateString('nl-BE')}
                    </span>
                    {!isOwn && (
                      <button
                        onClick={() => handleDelete(org)}
                        disabled={deletingId === org.id}
                        aria-label={`${org.name} verwijderen`}
                        title="Apotheek verwijderen (GDPR)"
                        className="shrink-0 rounded-md p-1.5 text-subtle transition-colors hover:bg-danger-soft hover:text-danger disabled:opacity-50"
                      >
                        <Trash2 size={15} />
                      </button>
                    )}
                  </li>
                )
              })}
            </ul>
          )}
        </div>
      </div>
    </>
  )
}
