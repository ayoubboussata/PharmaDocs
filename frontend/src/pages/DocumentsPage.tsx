import { useCallback, useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { RefreshCw, Search, Download, Trash2, ChevronLeft, ChevronRight } from 'lucide-react'
import { api } from '../api/client'
import { DocumentUpload } from '../components/DocumentUpload'
import { PageHeader } from '../components/ui/PageHeader'
import { Badge } from '../components/ui/Badge'
import { Button } from '../components/ui/Button'
import type { DocumentDetail, DocumentStatus, DocumentSummary } from '../types'

const PAGE_SIZE = 10

const STATUS_META: Record<DocumentStatus, { label: string; tone: 'success' | 'danger' | 'warning' }> = {
  Processed: { label: 'Verwerkt', tone: 'success' },
  Failed: { label: 'Mislukt', tone: 'danger' },
  Pending: { label: 'In behandeling', tone: 'warning' },
}

function formatDate(value: string | null) {
  if (!value) return '—'
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? '—' : date.toLocaleDateString('nl-BE')
}

function formatAmount(amount: number | null, currency: string | null) {
  if (amount == null) return '—'
  return `${amount.toFixed(2)} ${currency ?? ''}`.trim()
}

export function DocumentsPage() {
  const navigate = useNavigate()
  const [documents, setDocuments] = useState<DocumentSummary[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<{ type: 'ok' | 'warn'; text: string } | null>(null)
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<'all' | DocumentStatus>('all')
  const [exporting, setExporting] = useState(false)
  const [selected, setSelected] = useState<Set<string>>(new Set())
  const [deletingId, setDeletingId] = useState<string | null>(null)
  const [page, setPage] = useState(1)

  const loadDocuments = useCallback(async () => {
    setError(null)
    try {
      const res = await api.get<DocumentSummary[]>('/documents')
      setDocuments(res.data)
    } catch {
      setError('Kon de documenten niet laden.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void loadDocuments()
  }, [loadDocuments])

  // Terug naar pagina 1 wanneer de filter/zoekopdracht wijzigt.
  useEffect(() => {
    setPage(1)
  }, [search, statusFilter])

  function handleUploaded(doc: DocumentDetail) {
    if (doc.status === 'Failed') {
      setNotice({
        type: 'warn',
        text: `"${doc.fileName}" is opgeslagen, maar de extractie mislukte: ${doc.errorMessage ?? 'onbekende fout'}.`,
      })
    } else {
      const supplier = doc.extractedInvoice?.supplierName
      setNotice({
        type: 'ok',
        text: `"${doc.fileName}" verwerkt${supplier ? ` — ${supplier}` : ''}.`,
      })
    }
    void loadDocuments()
  }

  async function handleExport() {
    setExporting(true)
    try {
      // Zijn er rijen aangevinkt, exporteer enkel die; anders alles.
      const ids = selected.size > 0 ? Array.from(selected) : undefined
      const res = await api.post('/documents/export', { ids }, { responseType: 'blob' })
      const url = URL.createObjectURL(res.data as Blob)
      const link = document.createElement('a')
      link.href = url
      link.download = `pharmadocs-facturen-${new Date().toISOString().slice(0, 10)}.csv`
      document.body.appendChild(link)
      link.click()
      link.remove()
      URL.revokeObjectURL(url)
    } catch {
      setNotice({ type: 'warn', text: 'Exporteren mislukte. Probeer het opnieuw.' })
    } finally {
      setExporting(false)
    }
  }

  async function handleDelete(doc: DocumentSummary) {
    if (!window.confirm(`"${doc.fileName}" verwijderen? Dit kan niet ongedaan worden gemaakt.`)) return
    setDeletingId(doc.id)
    try {
      await api.delete(`/documents/${doc.id}`)
      setSelected((prev) => {
        const next = new Set(prev)
        next.delete(doc.id)
        return next
      })
      setNotice({ type: 'ok', text: `"${doc.fileName}" verwijderd.` })
      await loadDocuments()
    } catch {
      setNotice({ type: 'warn', text: 'Verwijderen mislukte. Probeer het opnieuw.' })
    } finally {
      setDeletingId(null)
    }
  }

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase()
    return documents.filter((doc) => {
      if (statusFilter !== 'all' && doc.status !== statusFilter) return false
      if (!q) return true
      return (
        doc.fileName.toLowerCase().includes(q) ||
        (doc.supplierName?.toLowerCase().includes(q) ?? false) ||
        (doc.invoiceNumber?.toLowerCase().includes(q) ?? false)
      )
    })
  }, [documents, search, statusFilter])

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE))
  const safePage = Math.min(page, totalPages)
  const paged = filtered.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE)

  const allPageSelected = paged.length > 0 && paged.every((d) => selected.has(d.id))

  function toggleSelect(id: string) {
    setSelected((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  function toggleSelectPage() {
    setSelected((prev) => {
      const next = new Set(prev)
      if (allPageSelected) paged.forEach((d) => next.delete(d.id))
      else paged.forEach((d) => next.add(d.id))
      return next
    })
  }

  return (
    <>
      <PageHeader
        title="Facturen"
        subtitle="Upload een factuur; de AI leest de gegevens automatisch uit."
        actions={
          <>
            <Button
              variant="secondary"
              size="sm"
              onClick={() => void handleExport()}
              disabled={exporting || documents.length === 0}
            >
              <Download size={15} />
              {exporting ? 'Exporteren…' : selected.size > 0 ? `Exporteren (${selected.size})` : 'Exporteren'}
            </Button>
            <Button variant="secondary" size="sm" onClick={() => void loadDocuments()}>
              <RefreshCw size={15} />
              Vernieuwen
            </Button>
          </>
        }
      />

      <div className="mx-auto max-w-5xl px-6 py-8 lg:px-8">
        <DocumentUpload onUploaded={handleUploaded} />

        {notice && (
          <p
            className={`mt-3 rounded-lg px-4 py-3 text-sm ${
              notice.type === 'ok' ? 'bg-success-soft text-success' : 'bg-warning-soft text-warning'
            }`}
          >
            {notice.text}
          </p>
        )}

        {/* Filters */}
        <div className="mb-3 mt-8 flex flex-wrap items-center gap-2">
          <div className="relative flex-1 sm:max-w-xs">
            <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-subtle" />
            <input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Zoek op leverancier, bestand of nr."
              className="w-full rounded-lg border border-line bg-surface py-2 pl-9 pr-3 text-sm text-fg placeholder:text-subtle transition-colors focus:border-accent"
            />
          </div>
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value as 'all' | DocumentStatus)}
            className="rounded-lg border border-line bg-surface px-3 py-2 text-sm text-fg transition-colors focus:border-accent"
          >
            <option value="all">Alle statussen</option>
            <option value="Processed">Verwerkt</option>
            <option value="Failed">Mislukt</option>
            <option value="Pending">In behandeling</option>
          </select>
          {selected.size > 0 && (
            <button
              onClick={() => setSelected(new Set())}
              className="text-sm text-muted underline-offset-2 hover:text-fg hover:underline"
            >
              Selectie wissen ({selected.size})
            </button>
          )}
        </div>

        {loading && <p className="text-muted">Laden…</p>}
        {error && <p className="rounded-lg bg-danger-soft px-4 py-3 text-danger">{error}</p>}

        {!loading && !error && documents.length === 0 && (
          <div className="rounded-2xl border border-dashed border-line-strong bg-surface p-12 text-center">
            <p className="text-fg">Nog geen documenten.</p>
            <p className="mt-1 text-sm text-muted">Upload hierboven een factuur om te beginnen.</p>
          </div>
        )}

        {!loading && !error && documents.length > 0 && filtered.length === 0 && (
          <div className="rounded-2xl border border-dashed border-line-strong bg-surface p-8 text-center">
            <p className="text-muted">Geen documenten voor deze filter.</p>
          </div>
        )}

        {!loading && !error && filtered.length > 0 && (
          <>
            <div className="overflow-x-auto rounded-2xl border border-line bg-surface shadow-card">
              <table className="w-full text-left text-sm">
                <thead>
                  <tr className="border-b border-line text-xs uppercase tracking-wide text-subtle">
                    <th className="px-4 py-3">
                      <input
                        type="checkbox"
                        aria-label="Selecteer deze pagina"
                        checked={allPageSelected}
                        onChange={toggleSelectPage}
                        style={{ accentColor: 'var(--accent)' }}
                        className="h-4 w-4 cursor-pointer align-middle"
                      />
                    </th>
                    <th className="px-4 py-3 font-medium">Bestand</th>
                    <th className="px-4 py-3 font-medium">Leverancier</th>
                    <th className="px-4 py-3 font-medium">Categorie</th>
                    <th className="px-4 py-3 font-medium">Factuurnr.</th>
                    <th className="px-4 py-3 font-medium">Datum</th>
                    <th className="px-4 py-3 font-medium">Totaal</th>
                    <th className="px-4 py-3 font-medium">Status</th>
                    <th className="px-2 py-3"></th>
                  </tr>
                </thead>
                <tbody>
                  {paged.map((doc) => {
                    const meta = STATUS_META[doc.status]
                    return (
                      <tr
                        key={doc.id}
                        onClick={() => navigate(`/documents/${doc.id}`)}
                        className="group cursor-pointer border-b border-line last:border-0 transition-colors hover:bg-elevated"
                      >
                        <td className="px-4 py-3" onClick={(e) => e.stopPropagation()}>
                          <input
                            type="checkbox"
                            aria-label={`Selecteer ${doc.fileName}`}
                            checked={selected.has(doc.id)}
                            onChange={() => toggleSelect(doc.id)}
                            style={{ accentColor: 'var(--accent)' }}
                            className="h-4 w-4 cursor-pointer align-middle"
                          />
                        </td>
                        <td className="px-4 py-3 font-medium text-fg">
                          <div className="max-w-[16ch] truncate" title={doc.fileName}>{doc.fileName}</div>
                        </td>
                        <td className="px-4 py-3 text-muted">
                          <div className="max-w-[18ch] truncate" title={doc.supplierName ?? ''}>{doc.supplierName ?? '—'}</div>
                        </td>
                        <td className="px-4 py-3">
                          {doc.category ? <Badge tone="accent">{doc.category}</Badge> : <span className="text-subtle">—</span>}
                        </td>
                        <td className="px-4 py-3 text-muted">
                          <div className="max-w-[16ch] truncate" title={doc.invoiceNumber ?? ''}>{doc.invoiceNumber ?? '—'}</div>
                        </td>
                        <td className="px-4 py-3 text-muted">{formatDate(doc.invoiceDate)}</td>
                        <td className="px-4 py-3 tabular-nums text-fg">
                          {formatAmount(doc.totalAmount, doc.currency)}
                        </td>
                        <td className="px-4 py-3">
                          <Badge tone={meta.tone}>{meta.label}</Badge>
                        </td>
                        <td className="px-2 py-3" onClick={(e) => e.stopPropagation()}>
                          <button
                            onClick={() => void handleDelete(doc)}
                            disabled={deletingId === doc.id}
                            aria-label={`Verwijder ${doc.fileName}`}
                            title="Verwijderen"
                            className="rounded-md p-1.5 text-subtle opacity-0 transition-all hover:bg-danger-soft hover:text-danger group-hover:opacity-100 disabled:opacity-50"
                          >
                            <Trash2 size={16} />
                          </button>
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>

            {/* Paginatie */}
            <div className="mt-4 flex items-center justify-between text-sm text-muted">
              <span>
                {filtered.length} {filtered.length === 1 ? 'factuur' : 'facturen'} · pagina {safePage} van {totalPages}
              </span>
              <div className="flex items-center gap-2">
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  disabled={safePage <= 1}
                >
                  <ChevronLeft size={15} />
                  Vorige
                </Button>
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                  disabled={safePage >= totalPages}
                >
                  Volgende
                  <ChevronRight size={15} />
                </Button>
              </div>
            </div>
          </>
        )}
      </div>
    </>
  )
}
