import { useEffect, useState } from 'react'
import { api } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import type { DocumentSummary } from '../types'

export function DocumentsPage() {
  const { email, logout } = useAuth()
  const [documents, setDocuments] = useState<DocumentSummary[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api
      .get<DocumentSummary[]>('/documents')
      .then((res) => setDocuments(res.data))
      .catch(() => setError('Kon de documenten niet laden.'))
      .finally(() => setLoading(false))
  }, [])

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
        <h2 className="mb-4 text-xl font-semibold text-white">Documenten</h2>

        {loading && <p className="text-slate-400">Laden…</p>}
        {error && <p className="rounded-lg bg-red-500/10 px-4 py-3 text-red-400">{error}</p>}

        {!loading && !error && documents.length === 0 && (
          <div className="rounded-2xl border border-dashed border-slate-700 bg-slate-900/50 p-12 text-center">
            <p className="text-slate-300">Nog geen documenten.</p>
            <p className="mt-1 text-sm text-slate-500">
              Uploaden en AI-extractie volgen in een volgende fase.
            </p>
          </div>
        )}

        {!loading && !error && documents.length > 0 && (
          <div className="overflow-hidden rounded-2xl border border-slate-800">
            <table className="w-full text-left text-sm">
              <thead className="bg-slate-900 text-slate-400">
                <tr>
                  <th className="px-4 py-3 font-medium">Bestand</th>
                  <th className="px-4 py-3 font-medium">Leverancier</th>
                  <th className="px-4 py-3 font-medium">Factuurnr.</th>
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
                    <td className="px-4 py-3">
                      {doc.totalAmount != null
                        ? `${doc.totalAmount.toFixed(2)} ${doc.currency ?? ''}`.trim()
                        : '—'}
                    </td>
                    <td className="px-4 py-3">
                      <span className="rounded-full bg-slate-800 px-2 py-0.5 text-xs text-slate-300">
                        {doc.status}
                      </span>
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
