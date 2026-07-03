import { useRef, useState, type DragEvent } from 'react'
import { AxiosError } from 'axios'
import { api } from '../api/client'
import type { DocumentDetail } from '../types'

interface DocumentUploadProps {
  /** Aangeroepen na een geslaagde upload met het verwerkte document. */
  onUploaded: (document: DocumentDetail) => void
}

const MAX_BYTES = 10 * 1024 * 1024 // 10 MB, gelijk aan de backend

export function DocumentUpload({ onUploaded }: DocumentUploadProps) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [dragging, setDragging] = useState(false)
  const [uploading, setUploading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function upload(file: File) {
    setError(null)

    if (file.type !== 'application/pdf' && !file.name.toLowerCase().endsWith('.pdf')) {
      setError('Enkel PDF-bestanden worden ondersteund.')
      return
    }
    if (file.size > MAX_BYTES) {
      setError('Bestand te groot (max. 10 MB).')
      return
    }

    const form = new FormData()
    form.append('file', file)

    setUploading(true)
    try {
      const res = await api.post<DocumentDetail>('/documents/upload', form)
      onUploaded(res.data)
    } catch (err) {
      const status = err instanceof AxiosError ? err.response?.status : undefined
      if (status === 415) setError('Enkel PDF-bestanden worden ondersteund.')
      else if (status === 413) setError('Bestand te groot (max. 10 MB).')
      else if (status === 400) setError('Geen geldig bestand ontvangen.')
      else setError('Uploaden mislukte. Probeer het opnieuw.')
    } finally {
      setUploading(false)
      if (inputRef.current) inputRef.current.value = ''
    }
  }

  function handleDrop(e: DragEvent<HTMLDivElement>) {
    e.preventDefault()
    setDragging(false)
    if (uploading) return
    const file = e.dataTransfer.files?.[0]
    if (file) void upload(file)
  }

  return (
    <div>
      <div
        onDragOver={(e) => {
          e.preventDefault()
          if (!uploading) setDragging(true)
        }}
        onDragLeave={() => setDragging(false)}
        onDrop={handleDrop}
        onClick={() => !uploading && inputRef.current?.click()}
        role="button"
        tabIndex={0}
        onKeyDown={(e) => {
          if ((e.key === 'Enter' || e.key === ' ') && !uploading) inputRef.current?.click()
        }}
        className={`flex cursor-pointer flex-col items-center justify-center rounded-2xl border-2 border-dashed px-6 py-10 text-center transition ${
          dragging
            ? 'border-emerald-500 bg-emerald-500/5'
            : 'border-slate-700 bg-slate-900/50 hover:border-slate-600'
        } ${uploading ? 'pointer-events-none opacity-70' : ''}`}
      >
        <input
          ref={inputRef}
          type="file"
          accept="application/pdf,.pdf"
          className="hidden"
          onChange={(e) => {
            const file = e.target.files?.[0]
            if (file) void upload(file)
          }}
        />

        {uploading ? (
          <div className="flex items-center gap-3 text-slate-300">
            <span className="h-5 w-5 animate-spin rounded-full border-2 border-slate-600 border-t-emerald-500" />
            <span className="text-sm">Bezig met uploaden en extraheren…</span>
          </div>
        ) : (
          <>
            <p className="text-sm font-medium text-slate-200">
              Sleep een factuur-PDF hierheen of <span className="text-emerald-400">kies een bestand</span>
            </p>
            <p className="mt-1 text-xs text-slate-500">PDF, max. 10 MB — de AI leest de gegevens automatisch uit</p>
          </>
        )}
      </div>

      {error && (
        <p className="mt-2 rounded-lg bg-red-500/10 px-3 py-2 text-sm text-red-400">{error}</p>
      )}
    </div>
  )
}
