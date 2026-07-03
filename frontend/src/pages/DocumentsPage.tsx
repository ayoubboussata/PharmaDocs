import { useCallback, useEffect, useState } from 'react'
import { api } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { DocumentUpload } from '../components/DocumentUpload'
import type { DocumentDetail, DocumentStatus, DocumentSummary } from '../types'

const STATUS_STYLES: Record<DocumentStatus, string> = {
  Processed: 'bg-emerald-500/15 text-emerald-400',
  Failed: 'bg-red-500/15 text-red-400',
  Pending: 'bg-amber-500/15 text-amber-400',
}

const STATUS_LABELS: Record<DocumentStatus, string> = {
  Processed: 'Verwerkt',
  Failed: 'Mislukt',
  Pending: 'In behandeling',
}

function StatusBadge({ status }: { status: DocumentStatus }) {
  return (
    <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${STATUS_STYLES[status]}`}>
      {STATUS_LABELS[status] ?? status}
    </span>
  )
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
  const { email, logout } = useAuth()
  const [documents, setDocuments] = useState<DocumentSummary[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<{ type: 'ok' | 'warn'; text: string } | null>(null)

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

  return (
    <div className="min-h-full bg-slate-950 text-slate-100">
      <header className="border-b border-slate-800 bg-slate-900">
        <div className="mx-auto flex max-w-5xl items-center justify-between px-6 py-4">
          <div>
            <h1 className="text-lg font-semibold text-white">PharmaDocs</h1>
            <p className="text-xs text-slate-400">Documentbeheer</p>
          </div>
          <div className="flex items-center gap-4">
            <span className="text-sm text-slate-400">{email}</span>
            <button
              onClick={logout}
              className="rounded-lg border border-slate-700 px-3 py-1.5 text-sm text-slate-300 transition hover:bg-slate-800"
            >
              Uitloggen
            </button>
          </div>
        </div>
      </header>

      <main className="mx-auto max-w-5xl px-6 py-8">
        <h2 className="mb-4 text-xl font-semibold text-white">Nieuwe factuur</h2>
        <DocumentUpload onUploaded={handleUploaded} />

        {notice && (
          <p
            className={`mt-3 rounded-lg px-4 py-3 text-sm ${
              notice.type === 'ok'
                ? 'bg-emerald-500/10 text-emerald-300'
                : 'bg-amber-500/10 text-amber-300'
            }`}
          >
            {notice.text}
          </p>
        )}

        <div className="mb-4 mt-10 flex items-center justify-between">
          <h2 className="text-xl font-semibold text-white">Documenten</h2>
          <button
            onClick={() => void loadDocuments()}
            className="rounded-lg border border-slate-700 px-3 py-1.5 text-sm text-slate-300 transition hover:bg-slate-800"
          >
            Vernieuwen
          </button>
        </div>

        {loading && <p className="text-slate-400">Laden…</p>}
        {error && <p className="rounded-lg bg-red-500/10 px-4 py-3 text-red-400">{error}</p>}

        {!loading && !error && documents.length === 0 && (
          <div className="rounded-2xl border border-dashed border-slate-700 bg-slate-900/50 p-12 text-center">
            <p className="text-slate-300">Nog geen documenten.</p>
            <p className="mt-1 text-sm text-slate-500">
              Upload hierboven een factuur om te beginnen.
            </p>
          </div>
        )}

        {!loading && !error && documents.length > 0 && (
          <div className="overflow-x-auto rounded-2xl border border-slate-800">
            <table className="w-full text-left text-sm">
              <thead className="bg-slate-900 text-slate-400">
                <tr>
                  <th className="px-4 py-3 font-medium">Bestand</th>
                  <th className="px-4 py-3 font-medium">Leverancier</th>
                  <th className="px-4 py-3 font-medium">Factuurnr.</th>
                  <th className="px-4 py-3 font-medium">Datum</th>
                  <th className="px-4 py-3 font-medium">Totaal</th>
                  <th className="px-4 py-3 font-medium">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800 bg-slate-900/40">
                {documents.map((doc) => (
                  <tr key={doc.id} className="text-slate-200">
                    <td className="px-4 py-3">{doc.fileName}</td>
                    <td className="px-4 py-3">{doc.supplierName ?? '—'}</td>
                    <td className="px-4 py-3">{doc.invoiceNumber ?? '—'}</td>
                    <td className="px-4 py-3">{formatDate(doc.invoiceDate)}</td>
                    <td className="px-4 py-3">{formatAmount(doc.totalAmount, doc.currency)}</td>
                    <td className="px-4 py-3">
                      <StatusBadge status={doc.status} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </main>
    </div>
  )
}
