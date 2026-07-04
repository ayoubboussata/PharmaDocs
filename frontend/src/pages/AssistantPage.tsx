import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react'
import { Send, FileText, Sparkles, BookText } from 'lucide-react'
import { api } from '../api/client'
import { ProcedureUpload } from '../components/ProcedureUpload'
import { PageHeader } from '../components/ui/PageHeader'
import { Badge } from '../components/ui/Badge'
import { Spinner } from '../components/ui/Spinner'
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
    } catch {
      setMessages((m) => [
        ...m,
        {
          id: nextId.current++,
          role: 'assistant',
          text: 'De assistent is even niet beschikbaar (mogelijk een rate-limit van de AI-dienst). Probeer het zo opnieuw.',
        },
      ])
    } finally {
      setAsking(false)
    }
  }

  return (
    <>
      <PageHeader
        title="Kennisassistent"
        subtitle="Vraag naar de interne procedures — met bronvermelding, zonder verzinsels."
      />

      <div className="mx-auto grid max-w-5xl gap-6 px-6 py-8 lg:grid-cols-[320px_1fr] lg:px-8">
        {/* Kennisbank */}
        <section className="lg:sticky lg:top-24 lg:self-start">
          <div className="mb-3 flex items-center gap-2">
            <BookText size={16} className="text-muted" />
            <h2 className="text-sm font-semibold text-fg">Kennisbank</h2>
          </div>

          <ProcedureUpload onIndexed={() => void loadSources()} />

          <div className="mt-3 space-y-1.5">
            {sources.length === 0 && (
              <p className="px-1 text-sm text-subtle">Nog geen procedures geïndexeerd.</p>
            )}
            {sources.map((s) => (
              <div
                key={s.sourceName}
                className="flex items-center gap-2 rounded-lg border border-line bg-surface px-3 py-2"
              >
                <FileText size={15} className="shrink-0 text-subtle" />
                <span className="min-w-0 flex-1 truncate text-sm text-fg" title={s.sourceName}>
                  {s.sourceName}
                </span>
                <span className="shrink-0 text-xs text-subtle">{s.chunkCount}</span>
              </div>
            ))}
          </div>
        </section>

        {/* Chat */}
        <section className="flex h-[calc(100vh-11rem)] min-h-[420px] flex-col overflow-hidden rounded-2xl border border-line bg-surface shadow-card">
          <div ref={scrollRef} className="flex-1 space-y-5 overflow-y-auto px-5 py-6">
            {messages.length === 0 && (
              <div className="flex h-full flex-col items-center justify-center text-center">
                <span className="mb-3 flex h-12 w-12 items-center justify-center rounded-2xl bg-accent-soft text-accent-text">
                  <Sparkles size={22} />
                </span>
                <p className="text-sm font-medium text-fg">Stel een vraag over de procedures</p>
                <p className="mt-1 text-sm text-subtle">Bv. “Wat zijn de openingsuren op zaterdag?”</p>
              </div>
            )}

            {messages.map((m) =>
              m.role === 'user' ? (
                <div key={m.id} className="flex justify-end">
                  <div className="max-w-[80%] rounded-2xl rounded-br-sm bg-accent px-4 py-2.5 text-sm text-accent-fg">
                    {m.text}
                  </div>
                </div>
              ) : (
                <div key={m.id} className="flex justify-start">
                  <div className="max-w-[85%] rounded-2xl rounded-bl-sm border border-line bg-elevated px-4 py-3">
                    <p className="whitespace-pre-wrap text-sm leading-relaxed text-fg">{m.text}</p>
                    {m.sources && m.sources.length > 0 && (
                      <div className="mt-3 flex flex-wrap items-center gap-1.5 border-t border-line pt-2.5">
                        <span className="text-xs text-subtle">Bronnen:</span>
                        {m.sources.map((src) => (
                          <Badge key={src} tone="accent">
                            {src}
                          </Badge>
                        ))}
                      </div>
                    )}
                  </div>
                </div>
              ),
            )}

            {asking && (
              <div className="flex justify-start">
                <div className="flex items-center gap-2 rounded-2xl rounded-bl-sm border border-line bg-elevated px-4 py-3 text-sm text-muted">
                  <Spinner className="h-4 w-4" />
                  Aan het zoeken…
                </div>
              </div>
            )}
          </div>

          <form onSubmit={handleAsk} className="flex gap-2 border-t border-line bg-surface px-4 py-3">
            <input
              value={question}
              onChange={(e) => setQuestion(e.target.value)}
              placeholder="Typ je vraag…"
              className="flex-1 rounded-lg border border-line bg-canvas px-3 py-2.5 text-sm text-fg placeholder:text-subtle transition-colors focus:border-accent"
            />
            <button
              type="submit"
              disabled={asking || !question.trim()}
              aria-label="Verstuur vraag"
              className="flex items-center justify-center rounded-lg bg-accent px-3.5 text-accent-fg transition-colors hover:bg-accent-hover disabled:cursor-not-allowed disabled:opacity-55"
            >
              <Send size={18} />
            </button>
          </form>
        </section>
      </div>
    </>
  )
}
