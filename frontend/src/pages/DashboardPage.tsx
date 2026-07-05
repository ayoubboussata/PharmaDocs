import { useCallback, useEffect, useState } from 'react'
import { RefreshCw, TrendingUp, FileText, Building2 } from 'lucide-react'
import { api } from '../api/client'
import { PageHeader } from '../components/ui/PageHeader'
import { Button } from '../components/ui/Button'
import type { DashboardSummary, SpendByLabel } from '../types'

function useMoney(currency: string) {
  return (amount: number) =>
    new Intl.NumberFormat('nl-BE', { style: 'currency', currency: currency || 'EUR' }).format(amount)
}

function monthLabel(month: string) {
  const date = new Date(`${month}-01T00:00:00`)
  return Number.isNaN(date.getTime())
    ? month
    : date.toLocaleDateString('nl-BE', { month: 'short', year: 'numeric' })
}

/** Horizontale balkenlijst: elk item een balk in verhouding tot de grootste waarde. */
function BarList({
  items,
  format,
}: {
  items: { label: string; value: number; sub?: string }[]
  format: (n: number) => string
}) {
  const max = Math.max(...items.map((i) => i.value), 1)
  return (
    <div className="space-y-3">
      {items.map((item) => (
        <div key={item.label}>
          <div className="mb-1 flex items-baseline justify-between gap-3 text-sm">
            <span className="min-w-0 truncate text-fg" title={item.label}>
              {item.label}
              {item.sub && <span className="ml-1.5 text-subtle">{item.sub}</span>}
            </span>
            <span className="shrink-0 tabular-nums font-medium text-fg">{format(item.value)}</span>
          </div>
          <div className="h-2 overflow-hidden rounded-full bg-elevated">
            <div
              className="h-full rounded-full bg-accent transition-[width] duration-500"
              style={{ width: `${Math.max((item.value / max) * 100, 2)}%` }}
            />
          </div>
        </div>
      ))}
    </div>
  )
}

const cardClass = 'rounded-2xl border border-line bg-surface p-6 shadow-card'

export function DashboardPage() {
  const [data, setData] = useState<DashboardSummary | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setError(null)
    try {
      const res = await api.get<DashboardSummary>('/dashboard')
      setData(res.data)
    } catch {
      setError('Kon het dashboard niet laden.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  const money = useMoney(data?.currency ?? 'EUR')
  const topSupplier = data?.bySupplier[0] as SpendByLabel | undefined

  return (
    <>
      <PageHeader
        title="Dashboard"
        subtitle="Overzicht van je uitgaven per leverancier, maand en categorie."
        actions={
          <Button variant="secondary" size="sm" onClick={() => void load()}>
            <RefreshCw size={15} />
            Vernieuwen
          </Button>
        }
      />

      <div className="mx-auto max-w-5xl px-6 py-8 lg:px-8">
        {loading && <p className="text-muted">Laden…</p>}
        {error && <p className="rounded-lg bg-danger-soft px-4 py-3 text-danger">{error}</p>}

        {data && !loading && data.invoiceCount === 0 && (
          <div className="rounded-2xl border border-dashed border-line-strong bg-surface p-12 text-center">
            <p className="text-fg">Nog geen verwerkte facturen.</p>
            <p className="mt-1 text-sm text-muted">
              Upload facturen bij <span className="text-fg">Facturen</span> — hier verschijnen dan de cijfers.
            </p>
          </div>
        )}

        {data && !loading && data.invoiceCount > 0 && (
          <div className="space-y-6">
            {/* KPI-kaarten */}
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
              <div className={cardClass}>
                <div className="flex items-center gap-2 text-sm text-muted">
                  <TrendingUp size={16} className="text-accent" /> Totaal uitgegeven
                </div>
                <p className="mt-2 text-2xl font-semibold tabular-nums text-fg">{money(data.totalSpend)}</p>
              </div>
              <div className={cardClass}>
                <div className="flex items-center gap-2 text-sm text-muted">
                  <FileText size={16} className="text-accent" /> Verwerkte facturen
                </div>
                <p className="mt-2 text-2xl font-semibold tabular-nums text-fg">{data.invoiceCount}</p>
              </div>
              <div className={cardClass}>
                <div className="flex items-center gap-2 text-sm text-muted">
                  <Building2 size={16} className="text-accent" /> Grootste leverancier
                </div>
                <p className="mt-2 truncate text-2xl font-semibold text-fg" title={topSupplier?.label}>
                  {topSupplier?.label ?? '—'}
                </p>
                {topSupplier && <p className="text-sm text-muted">{money(topSupplier.total)}</p>}
              </div>
            </div>

            {/* Per maand */}
            <section className={cardClass}>
              <h3 className="mb-5 text-sm font-semibold text-fg">Uitgaven per maand</h3>
              {data.byMonth.length === 0 ? (
                <p className="text-sm text-subtle">Geen facturen met een datum.</p>
              ) : (
                <BarList
                  items={data.byMonth.map((m) => ({ label: monthLabel(m.month), value: m.total }))}
                  format={money}
                />
              )}
            </section>

            {/* Per leverancier + per categorie */}
            <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
              <section className={cardClass}>
                <h3 className="mb-5 text-sm font-semibold text-fg">Uitgaven per leverancier</h3>
                <BarList
                  items={data.bySupplier.map((s) => ({
                    label: s.label,
                    value: s.total,
                    sub: `· ${s.count}`,
                  }))}
                  format={money}
                />
              </section>
              <section className={cardClass}>
                <h3 className="mb-5 text-sm font-semibold text-fg">Uitgaven per categorie</h3>
                <BarList
                  items={data.byCategory.map((c) => ({
                    label: c.label,
                    value: c.total,
                    sub: `· ${c.count}`,
                  }))}
                  format={money}
                />
              </section>
            </div>
          </div>
        )}
      </div>
    </>
  )
}
