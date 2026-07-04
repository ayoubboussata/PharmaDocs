import { useCallback, useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { RefreshCw, Search, ChevronRight } from 'lucide-react'
import { api } from '../api/client'
import { DocumentUpload } from '../components/DocumentUpload'
import { PageHeader } from '../components/ui/PageHeader'
import { Badge } from '../components/ui/Badge'
import { Button } from '../components/ui/Button'
import type { DocumentDetail, DocumentStatus, DocumentSummary } from '../types'

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

  return (
    <>
      <PageHeader
        title="Facturen"
        subtitle="Upload een factuur; de AI leest de gegevens automatisch uit."
        actions={
          <Button variant="secondary" size="sm" onClick={() => void loadDocuments()}>
            <RefreshCw size={15} />
            Vernieuwen
          </Button>
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
          <div className="overflow-hidden rounded-2xl border border-line bg-surface shadow-card">
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-line text-xs uppercase tracking-wide text-subtle">
                  <th className="px-4 py-3 font-medium">Bestand</th>
                  <th className="px-4 py-3 font-medium">Leverancier</th>
                  <th className="px-4 py-3 font-medium">Factuurnr.</th>
                  <th className="px-4 py-3 font-medium">Datum</th>
                  <th className="px-4 py-3 font-medium">Totaal</th>
                  <th className="px-4 py-3 font-medium">Status</th>
                  <th className="px-2 py-3"></th>
                </tr>
              </thead>
              <tbody>
                {filtered.map((doc) => {
                  const meta = STATUS_META[doc.status]
                  return (
                    <tr
                      key={doc.id}
                      onClick={() => navigate(`/documents/${doc.id}`)}
                      className="group cursor-pointer border-b border-line last:border-0 transition-colors hover:bg-elevated"
                    >
                      <td className="px-4 py-3 font-medium text-fg">{doc.fileName}</td>
                      <td className="px-4 py-3 text-muted">{doc.supplierName ?? '—'}</td>
                      <td className="px-4 py-3 text-muted">{doc.invoiceNumber ?? '—'}</td>
                      <td className="px-4 py-3 text-muted">{formatDate(doc.invoiceDate)}</td>
                      <td className="px-4 py-3 tabular-nums text-fg">
                        {formatAmount(doc.totalAmount, doc.currency)}
                      </td>
                      <td className="px-4 py-3">
                        <Badge tone={meta.tone}>{meta.label}</Badge>
                      </td>
                      <td className="px-2 py-3 text-subtle">
                        <ChevronRight
                          size={16}
                          className="opacity-0 transition-opacity group-hover:opacity-100"
                        />
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </>
  )
}
