import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react'
import { AxiosError } from 'axios'
import { api } from '../api/client'
import { AppHeader } from '../components/AppHeader'
import { ProcedureUpload } from '../components/ProcedureUpload'
import type { AskResponse, KnowledgeSource } from '../types'

interface ChatMessage {
  id: number
  role: 'user' | 'assistant'
  text: string
  sources?: string[]
}

export function AssistantPage() {
  const [sources, setSources] = useState<KnowledgeSource[]>([])
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [question, setQuestion] = useState('')
  const [asking, setAsking] = useState(false)

  const nextId = useRef(1)
  const scrollRef = useRef<HTMLDivElement>(null)

  const loadSources = useCallback(async () => {
    try {
      const res = await api.get<KnowledgeSource[]>('/knowledge/sources')
      setSources(res.data)
    } catch {
      // Stil: de kennisbank-lijst is bijzaak; de chat blijft bruikbaar.
    }
  }, [])

  useEffect(() => {
    void loadSources()
  }, [loadSources])

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: 'smooth' })
  }, [messages, asking])

  async function handleAsk(e: FormEvent) {
    e.preventDefault()
    const q = question.trim()
    if (!q || asking) return

    setMessages((m) => [...m, { id: nextId.current++, role: 'user', text: q }])
    setQuestion('')
    setAsking(true)
    try {
      const res = await api.post<AskResponse>('/knowledge/ask', { question: q })
      setMessages((m) => [
        ...m,
        { id: nextId.current++, role: 'assistant', text: res.data.answer, sources: res.data.sources },
      ])
    } catch (err) {
      const status = err instanceof AxiosError ? err.response?.status : undefined
      const text =
        status === 503
          ? 'De assistent is even niet beschikbaar (mogelijk een rate-limit van de AI-dienst). Probeer het zo opnieuw.'
          : 'Er ging iets mis bij het beantwoorden. Probeer het opnieuw.'
      setMessages((m) => [...m, { id: nextId.current++, role: 'assistant', text }])
    } finally {
      setAsking(false)
    }
  }

  return (
    <div className="min-h-full bg-slate-950 text-slate-100">
      <AppHeader />

      <main className="mx-auto grid max-w-5xl gap-8 px-6 py-8 lg:grid-cols-[320px_1fr]">
        {/* Kennisbank (procedures) */}
        <section>
          <h2 className="mb-1 text-lg font-semibold text-white">Kennisbank</h2>
          <p className="mb-4 text-sm text-slate-400">
            De procedures waarover de assistent kan antwoorden.
          </p>

          <ProcedureUpload onIndexed={() => void loadSources()} />

          <div className="mt-4 space-y-2">
            {sources.length === 0 && (
              <p className="text-sm text-slate-500">Nog geen procedures geïndexeerd.</p>
            )}
            {sources.map((s) => (
              <div
                key={s.sourceName}
                className="flex items-center justify-between rounded-lg border border-slate-800 bg-slate-900/40 px-3 py-2"
              >
                <span className="truncate text-sm text-slate-200" title={s.sourceName}>
                  {s.sourceName}
                </span>
                <span className="ml-2 shrink-0 rounded-full bg-slate-800 px-2 py-0.5 text-xs text-slate-400">
                  {s.chunkCount} stukjes
                </span>
              </div>
            ))}
          </div>
        </section>

        {/* Chat */}
        <section className="flex min-h-[70vh] flex-col rounded-2xl border border-slate-800 bg-slate-900/40">
          <div className="border-b border-slate-800 px-5 py-3">
            <h2 className="text-lg font-semibold text-white">Vraag de assistent</h2>
            <p className="text-xs text-slate-400">
              Antwoorden komen enkel uit de procedures, met bronvermelding.
            </p>
          </div>

          <div ref={scrollRef} className="flex-1 space-y-4 overflow-y-auto px-5 py-5">
            {messages.length === 0 && (
              <div className="mt-10 text-center text-sm text-slate-500">
                <p>Stel een vraag over de interne procedures.</p>
                <p className="mt-1">Bv. “Wat zijn de openingsuren op zaterdag?”</p>
              </div>
            )}

            {messages.map((m) =>
              m.role === 'user' ? (
                <div key={m.id} className="flex justify-end">
                  <div className="max-w-[80%] rounded-2xl rounded-br-sm bg-emerald-600 px-4 py-2 text-sm text-white">
                    {m.text}
                  </div>
                </div>
              ) : (
                <div key={m.id} className="flex justify-start">
                  <div className="max-w-[85%] rounded-2xl rounded-bl-sm border border-slate-800 bg-slate-900 px-4 py-3">
                    <p className="whitespace-pre-wrap text-sm text-slate-100">{m.text}</p>
                    {m.sources && m.sources.length > 0 && (
                      <div className="mt-3 flex flex-wrap gap-1.5 border-t border-slate-800 pt-2">
                        <span className="text-xs text-slate-500">Bronnen:</span>
                        {m.sources.map((src) => (
                          <span
                            key={src}
                            className="rounded-full bg-slate-800 px-2 py-0.5 text-xs text-slate-300"
                          >
                            {src}
                          </span>
                        ))}
                      </div>
                    )}
                  </div>
                </div>
              ),
            )}

            {asking && (
              <div className="flex justify-start">
                <div className="flex items-center gap-2 rounded-2xl rounded-bl-sm border border-slate-800 bg-slate-900 px-4 py-3 text-sm text-slate-400">
                  <span className="h-4 w-4 animate-spin rounded-full border-2 border-slate-700 border-t-emerald-500" />
                  Aan het zoeken…
                </div>
              </div>
            )}
          </div>

          <form onSubmit={handleAsk} className="flex gap-2 border-t border-slate-800 px-5 py-3">
            <input
              value={question}
              onChange={(e) => setQuestion(e.target.value)}
              placeholder="Typ je vraag…"
              className="flex-1 rounded-lg border border-slate-700 bg-slate-800 px-3 py-2 text-sm text-white outline-none focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500"
            />
            <button
              type="submit"
              disabled={asking || !question.trim()}
              className="rounded-lg bg-emerald-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-emerald-500 disabled:cursor-not-allowed disabled:opacity-60"
            >
              Vraag
            </button>
          </form>
        </section>
      </main>
    </div>
  )
}
