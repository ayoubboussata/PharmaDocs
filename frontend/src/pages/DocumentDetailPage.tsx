import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { AxiosError } from 'axios'
import { api } from '../api/client'
import type { DocumentDetail } from '../types'

/** Bewerkbaar model: numerieke velden als string voor vlot typen. */
interface LineForm {
  description: string
  quantity: string
  unitPrice: string
  lineTotal: string
}

interface InvoiceForm {
  supplierName: string
  invoiceNumber: string
  invoiceDate: string
  subtotalAmount: string
  vatRate: string
  vatAmount: string
  totalAmount: string
  currency: string
  lineItems: LineForm[]
}

const inputClass =
  'w-full rounded-lg border border-slate-700 bg-slate-800 px-3 py-2 text-sm text-white outline-none focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500'

function toForm(doc: DocumentDetail): InvoiceForm {
  const inv = doc.extractedInvoice!
  return {
    supplierName: inv.supplierName,
    invoiceNumber: inv.invoiceNumber,
    invoiceDate: inv.invoiceDate ?? '',
    subtotalAmount: String(inv.subtotalAmount),
    vatRate: inv.vatRate == null ? '' : String(inv.vatRate),
    vatAmount: String(inv.vatAmount),
    totalAmount: String(inv.totalAmount),
    currency: inv.currency,
    lineItems: inv.lineItems.map((l) => ({
      description: l.description,
      quantity: String(l.quantity),
      unitPrice: String(l.unitPrice),
      lineTotal: String(l.lineTotal),
    })),
  }
}

const num = (v: string) => {
  const n = Number.parseFloat(v.replace(',', '.'))
  return Number.isFinite(n) ? n : 0
}

export function DocumentDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()

  const [doc, setDoc] = useState<DocumentDetail | null>(null)
  const [form, setForm] = useState<InvoiceForm | null>(null)
  const [loading, setLoading] = useState(true)
  const [notFound, setNotFound] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)

  const load = useCallback(async () => {
    if (!id) return
    setLoading(true)
    setNotFound(false)
    try {
      const res = await api.get<DocumentDetail>(`/documents/${id}`)
      setDoc(res.data)
      if (res.data.extractedInvoice) setForm(toForm(res.data))
    } catch (err) {
      if (err instanceof AxiosError && err.response?.status === 404) setNotFound(true)
      else setError('Kon het document niet laden.')
    } finally {
      setLoading(false)
    }
  }, [id])

  useEffect(() => {
    void load()
  }, [load])

  const lineSum = useMemo(
    () => (form ? form.lineItems.reduce((s, l) => s + num(l.lineTotal), 0) : 0),
    [form],
  )

  function updateLine(index: number, field: keyof LineForm, value: string) {
    setForm((f) => {
      if (!f) return f
      const lineItems = f.lineItems.map((l, i) => (i === index ? { ...l, [field]: value } : l))
      return { ...f, lineItems }
    })
    setSaved(false)
  }

  function addLine() {
    setForm((f) =>
      f ? { ...f, lineItems: [...f.lineItems, { description: '', quantity: '', unitPrice: '', lineTotal: '' }] } : f,
    )
    setSaved(false)
  }

  function removeLine(index: number) {
    setForm((f) => (f ? { ...f, lineItems: f.lineItems.filter((_, i) => i !== index) } : f))
    setSaved(false)
  }

  function setField<K extends keyof InvoiceForm>(field: K, value: InvoiceForm[K]) {
    setForm((f) => (f ? { ...f, [field]: value } : f))
    setSaved(false)
  }

  async function handleSave() {
    if (!form || !id) return
    setSaving(true)
    setError(null)
    setSaved(false)
    try {
      const body = {
        supplierName: form.supplierName,
        invoiceNumber: form.invoiceNumber,
        invoiceDate: form.invoiceDate ? form.invoiceDate : null,
        subtotalAmount: num(form.subtotalAmount),
        vatRate: form.vatRate.trim() === '' ? null : num(form.vatRate),
        vatAmount: num(form.vatAmount),
        totalAmount: num(form.totalAmount),
        currency: form.currency,
        lineItems: form.lineItems.map((l) => ({
          description: l.description,
          quantity: num(l.quantity),
          unitPrice: num(l.unitPrice),
          lineTotal: num(l.lineTotal),
        })),
      }
      const res = await api.put<DocumentDetail>(`/documents/${id}/invoice`, body)
      setDoc(res.data)
      if (res.data.extractedInvoice) setForm(toForm(res.data))
      setSaved(true)
    } catch (err) {
      const status = err instanceof AxiosError ? err.response?.status : undefined
      if (status === 400) setError('Controleer de ingevulde velden.')
      else setError('Opslaan mislukte. Probeer het opnieuw.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="min-h-full bg-slate-950 text-slate-100">
      <header className="border-b border-slate-800 bg-slate-900">
        <div className="mx-auto flex max-w-4xl items-center justify-between px-6 py-4">
          <div>
            <h1 className="text-lg font-semibold text-white">PharmaDocs</h1>
            <p className="text-xs text-slate-400">Documentdetail</p>
          </div>
          <Link
            to="/documents"
            className="rounded-lg border border-slate-700 px-3 py-1.5 text-sm text-slate-300 transition hover:bg-slate-800"
          >
            ← Terug naar overzicht
          </Link>
        </div>
      </header>

      <main className="mx-auto max-w-4xl px-6 py-8">
        {loading && <p className="text-slate-400">Laden…</p>}

        {notFound && (
          <div className="rounded-2xl border border-slate-800 bg-slate-900/50 p-8 text-center">
            <p className="text-slate-300">Document niet gevonden.</p>
            <button
              onClick={() => navigate('/documents')}
              className="mt-4 rounded-lg bg-emerald-600 px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-500"
            >
              Naar overzicht
            </button>
          </div>
        )}

        {doc && !loading && (
          <>
            <div className="mb-6">
              <h2 className="text-xl font-semibold text-white">{doc.fileName}</h2>
              <p className="mt-1 text-sm text-slate-400">
                {(doc.fileSizeBytes / 1024).toFixed(0)} kB · geüpload{' '}
                {new Date(doc.uploadedAt).toLocaleString('nl-BE')}
              </p>
            </div>

            {/* Document zonder extractie (Failed/Pending) → geen correctieformulier */}
            {!doc.extractedInvoice && (
              <div className="rounded-2xl border border-slate-800 bg-slate-900/50 p-6">
                <p className="text-slate-300">
                  Dit document heeft (nog) geen geëxtraheerde factuur.
                </p>
                {doc.errorMessage && (
                  <p className="mt-2 rounded-lg bg-red-500/10 px-3 py-2 text-sm text-red-400">
                    {doc.errorMessage}
                  </p>
                )}
              </div>
            )}

            {form && (
              <form
                onSubmit={(e) => {
                  e.preventDefault()
                  void handleSave()
                }}
                className="space-y-6"
              >
                {/* Factuurkop */}
                <section className="rounded-2xl border border-slate-800 bg-slate-900/50 p-6">
                  <h3 className="mb-4 text-sm font-semibold uppercase tracking-wide text-slate-400">
                    Factuurgegevens
                  </h3>
                  <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                    <label className="block">
                      <span className="mb-1 block text-sm text-slate-300">Leverancier</span>
                      <input
                        className={inputClass}
                        value={form.supplierName}
                        onChange={(e) => setField('supplierName', e.target.value)}
                        required
                      />
                    </label>
                    <label className="block">
                      <span className="mb-1 block text-sm text-slate-300">Factuurnummer</span>
                      <input
                        className={inputClass}
                        value={form.invoiceNumber}
                        onChange={(e) => setField('invoiceNumber', e.target.value)}
                        required
                      />
                    </label>
                    <label className="block">
                      <span className="mb-1 block text-sm text-slate-300">Factuurdatum</span>
                      <input
                        type="date"
                        className={inputClass}
                        value={form.invoiceDate}
                        onChange={(e) => setField('invoiceDate', e.target.value)}
                      />
                    </label>
                    <label className="block">
                      <span className="mb-1 block text-sm text-slate-300">Munt</span>
                      <input
                        className={inputClass}
                        value={form.currency}
                        onChange={(e) => setField('currency', e.target.value)}
                        maxLength={8}
                      />
                    </label>
                  </div>

                  {/* Bedragen: subtotaal → btw → totaal */}
                  <div className="mt-4 grid grid-cols-2 gap-4 sm:grid-cols-4">
                    <label className="block">
                      <span className="mb-1 block text-sm text-slate-300">Subtotaal (excl. btw)</span>
                      <input
                        inputMode="decimal"
                        className={inputClass}
                        value={form.subtotalAmount}
                        onChange={(e) => setField('subtotalAmount', e.target.value)}
                      />
                    </label>
                    <label className="block">
                      <span className="mb-1 block text-sm text-slate-300">Btw-tarief %</span>
                      <input
                        inputMode="decimal"
                        placeholder="—"
                        className={inputClass}
                        value={form.vatRate}
                        onChange={(e) => setField('vatRate', e.target.value)}
                      />
                    </label>
                    <label className="block">
                      <span className="mb-1 block text-sm text-slate-300">Btw-bedrag</span>
                      <input
                        inputMode="decimal"
                        className={inputClass}
                        value={form.vatAmount}
                        onChange={(e) => setField('vatAmount', e.target.value)}
                      />
                    </label>
                    <label className="block">
                      <span className="mb-1 block text-sm text-slate-300">Totaal (incl. btw)</span>
                      <input
                        inputMode="decimal"
                        className={inputClass}
                        value={form.totalAmount}
                        onChange={(e) => setField('totalAmount', e.target.value)}
                      />
                    </label>
                  </div>

                  {Math.abs(num(form.subtotalAmount) + num(form.vatAmount) - num(form.totalAmount)) > 0.01 && (
                    <p className="mt-3 text-sm text-amber-400">
                      Let op: subtotaal + btw ({(num(form.subtotalAmount) + num(form.vatAmount)).toFixed(2)}) wijkt af van het totaal ({num(form.totalAmount).toFixed(2)}).
                    </p>
                  )}
                </section>

                {/* Lijnitems */}
                <section className="rounded-2xl border border-slate-800 bg-slate-900/50 p-6">
                  <div className="mb-4 flex items-center justify-between">
                    <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-400">
                      Lijnitems
                    </h3>
                    <button
                      type="button"
                      onClick={addLine}
                      className="rounded-lg border border-slate-700 px-3 py-1.5 text-sm text-slate-200 hover:bg-slate-800"
                    >
                      + Lijn toevoegen
                    </button>
                  </div>

                  {form.lineItems.length === 0 && (
                    <p className="text-sm text-slate-500">Nog geen lijnen. Voeg er een toe.</p>
                  )}

                  <div className="space-y-3">
                    {form.lineItems.map((line, i) => (
                      <div key={i} className="grid grid-cols-12 gap-2">
                        <input
                          className={`${inputClass} col-span-12 sm:col-span-6`}
                          placeholder="Omschrijving"
                          value={line.description}
                          onChange={(e) => updateLine(i, 'description', e.target.value)}
                          required
                        />
                        <input
                          className={`${inputClass} col-span-3 sm:col-span-2`}
                          inputMode="decimal"
                          placeholder="Aantal"
                          value={line.quantity}
                          onChange={(e) => updateLine(i, 'quantity', e.target.value)}
                        />
                        <input
                          className={`${inputClass} col-span-4 sm:col-span-2`}
                          inputMode="decimal"
                          placeholder="Prijs"
                          value={line.unitPrice}
                          onChange={(e) => updateLine(i, 'unitPrice', e.target.value)}
                        />
                        <input
                          className={`${inputClass} col-span-4 sm:col-span-1`}
                          inputMode="decimal"
                          placeholder="Totaal"
                          value={line.lineTotal}
                          onChange={(e) => updateLine(i, 'lineTotal', e.target.value)}
                        />
                        <button
                          type="button"
                          onClick={() => removeLine(i)}
                          aria-label="Lijn verwijderen"
                          className="col-span-1 rounded-lg border border-slate-700 text-slate-400 hover:bg-red-500/10 hover:text-red-400"
                        >
                          ✕
                        </button>
                      </div>
                    ))}
                  </div>

                  <p className="mt-4 text-sm text-slate-400">
                    Som van de lijntotalen: <span className="text-slate-200">{lineSum.toFixed(2)}</span>
                    {Math.abs(lineSum - num(form.subtotalAmount)) > 0.01 && (
                      <span className="ml-2 text-amber-400">
                        (wijkt af van het subtotaal {num(form.subtotalAmount).toFixed(2)})
                      </span>
                    )}
                  </p>
                </section>

                <div className="flex items-center gap-4">
                  <button
                    type="submit"
                    disabled={saving}
                    className="rounded-lg bg-emerald-600 px-5 py-2 text-sm font-semibold text-white transition hover:bg-emerald-500 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    {saving ? 'Opslaan…' : 'Wijzigingen opslaan'}
                  </button>
                  {saved && <span className="text-sm text-emerald-400">Opgeslagen ✓</span>}
                  {error && <span className="text-sm text-red-400">{error}</span>}
                </div>
              </form>
            )}
          </>
        )}
      </main>
    </div>
  )
}
