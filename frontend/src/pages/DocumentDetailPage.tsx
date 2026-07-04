import { useCallback, useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { AxiosError } from 'axios'
import { ArrowLeft, Plus, X, Check } from 'lucide-react'
import { api } from '../api/client'
import { PageHeader } from '../components/ui/PageHeader'
import { Button } from '../components/ui/Button'
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
  'w-full rounded-lg border border-line bg-canvas px-3 py-2 text-sm text-fg placeholder:text-subtle transition-colors focus:border-accent'
const labelClass = 'mb-1.5 block text-sm text-muted'
const cardClass = 'rounded-2xl border border-line bg-surface p-6 shadow-card'

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

  const totalMismatch =
    form && Math.abs(num(form.subtotalAmount) + num(form.vatAmount) - num(form.totalAmount)) > 0.01
  const sumMismatch = form && Math.abs(lineSum - num(form.subtotalAmount)) > 0.01

  return (
    <>
      <PageHeader
        title={doc?.fileName ?? 'Documentdetail'}
        subtitle={
          doc ? `${(doc.fileSizeBytes / 1024).toFixed(0)} kB · geüpload ${new Date(doc.uploadedAt).toLocaleString('nl-BE')}` : undefined
        }
        actions={
          <Button variant="secondary" size="sm" onClick={() => navigate('/documents')}>
            <ArrowLeft size={15} />
            Terug
          </Button>
        }
      />

      <div className="mx-auto max-w-3xl px-6 py-8 lg:px-8">
        {loading && <p className="text-muted">Laden…</p>}

        {notFound && (
          <div className="rounded-2xl border border-line bg-surface p-10 text-center shadow-card">
            <p className="text-fg">Document niet gevonden.</p>
            <Button className="mt-4" onClick={() => navigate('/documents')}>
              Naar overzicht
            </Button>
          </div>
        )}

        {doc && !loading && !doc.extractedInvoice && (
          <div className={cardClass}>
            <p className="text-fg">Dit document heeft (nog) geen geëxtraheerde factuur.</p>
            {doc.errorMessage && (
              <p className="mt-2 rounded-lg bg-danger-soft px-3 py-2 text-sm text-danger">
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
            {/* Factuurgegevens */}
            <section className={cardClass}>
              <h3 className="mb-5 text-sm font-semibold text-fg">Factuurgegevens</h3>
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <label>
                  <span className={labelClass}>Leverancier</span>
                  <input
                    className={inputClass}
                    value={form.supplierName}
                    onChange={(e) => setField('supplierName', e.target.value)}
                    required
                  />
                </label>
                <label>
                  <span className={labelClass}>Factuurnummer</span>
                  <input
                    className={inputClass}
                    value={form.invoiceNumber}
                    onChange={(e) => setField('invoiceNumber', e.target.value)}
                    required
                  />
                </label>
                <label>
                  <span className={labelClass}>Factuurdatum</span>
                  <input
                    type="date"
                    className={inputClass}
                    value={form.invoiceDate}
                    onChange={(e) => setField('invoiceDate', e.target.value)}
                  />
                </label>
                <label>
                  <span className={labelClass}>Munt</span>
                  <input
                    className={inputClass}
                    value={form.currency}
                    onChange={(e) => setField('currency', e.target.value)}
                    maxLength={8}
                  />
                </label>
              </div>

              <div className="mt-4 grid grid-cols-2 gap-4 sm:grid-cols-4">
                <label>
                  <span className={labelClass}>Subtotaal</span>
                  <input
                    inputMode="decimal"
                    className={inputClass}
                    value={form.subtotalAmount}
                    onChange={(e) => setField('subtotalAmount', e.target.value)}
                  />
                </label>
                <label>
                  <span className={labelClass}>Btw-tarief %</span>
                  <input
                    inputMode="decimal"
                    placeholder="—"
                    className={inputClass}
                    value={form.vatRate}
                    onChange={(e) => setField('vatRate', e.target.value)}
                  />
                </label>
                <label>
                  <span className={labelClass}>Btw-bedrag</span>
                  <input
                    inputMode="decimal"
                    className={inputClass}
                    value={form.vatAmount}
                    onChange={(e) => setField('vatAmount', e.target.value)}
                  />
                </label>
                <label>
                  <span className={labelClass}>Totaal (incl.)</span>
                  <input
                    inputMode="decimal"
                    className={inputClass}
                    value={form.totalAmount}
                    onChange={(e) => setField('totalAmount', e.target.value)}
                  />
                </label>
              </div>

              {totalMismatch && (
                <p className="mt-3 text-sm text-warning">
                  Let op: subtotaal + btw ({(num(form.subtotalAmount) + num(form.vatAmount)).toFixed(2)}) wijkt af van het totaal ({num(form.totalAmount).toFixed(2)}).
                </p>
              )}
            </section>

            {/* Lijnitems */}
            <section className={cardClass}>
              <div className="mb-4 flex items-center justify-between">
                <h3 className="text-sm font-semibold text-fg">Lijnitems</h3>
                <Button type="button" variant="secondary" size="sm" onClick={addLine}>
                  <Plus size={15} />
                  Lijn toevoegen
                </Button>
              </div>

              {form.lineItems.length === 0 && (
                <p className="text-sm text-subtle">Nog geen lijnen. Voeg er een toe.</p>
              )}

              <div className="space-y-2.5">
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
                      className="col-span-1 flex items-center justify-center rounded-lg border border-line text-subtle transition-colors hover:bg-danger-soft hover:text-danger"
                    >
                      <X size={15} />
                    </button>
                  </div>
                ))}
              </div>

              <p className="mt-4 text-sm text-muted">
                Som van de lijntotalen: <span className="tabular-nums text-fg">{lineSum.toFixed(2)}</span>
                {sumMismatch && (
                  <span className="ml-2 text-warning">
                    (wijkt af van het subtotaal {num(form.subtotalAmount).toFixed(2)})
                  </span>
                )}
              </p>
            </section>

            <div className="flex items-center gap-4">
              <Button type="submit" disabled={saving}>
                {saving ? 'Opslaan…' : 'Wijzigingen opslaan'}
              </Button>
              {saved && (
                <span className="flex items-center gap-1 text-sm text-success">
                  <Check size={15} /> Opgeslagen
                </span>
              )}
              {error && <span className="text-sm text-danger">{error}</span>}
            </div>
          </form>
        )}
      </div>
    </>
  )
}
