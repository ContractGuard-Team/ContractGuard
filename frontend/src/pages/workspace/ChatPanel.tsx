import { ArrowUp } from 'lucide-react'
import { useEffect, useRef, useState, type KeyboardEvent, type ReactNode } from 'react'
import { Button, cx } from '../../components/ui'
import { prefersReducedMotion } from '../../hooks'
import { clauseLabel, type ContractDetail } from '../../model'

type Msg = { role: 'user' | 'assistant'; text: string; shown: number; demoNote?: boolean }

const tokenize = (text: string) => text.split(/(\[\[[\d.]+\]\]|\s+)/).filter(Boolean)

// shortcut: khớp từ khóa với câu trả lời soạn sẵn thay cho LLM + RAG; thay bằng API chat (streaming) khi có backend.
function answerFor(detail: ContractDetail, question: string) {
  const q = question.toLocaleLowerCase('vi')
  const exact = detail.chat.find((c) => c.q.toLocaleLowerCase('vi') === q)
  if (exact) return { text: exact.a }
  const scored = detail.chat
    .map((c) => ({ c, score: c.keywords.filter((k) => q.includes(k)).length }))
    .sort((a, b) => b.score - a.score)[0]
  if (scored && scored.score > 0) return { text: scored.c.a }
  return { text: detail.chatFallback, demoNote: true }
}

export function ChatPanel({ detail, onJump }: { detail: ContractDetail; onJump: (clauseId: string) => void }) {
  const [msgs, setMsgs] = useState<Msg[]>([])
  const [input, setInput] = useState('')
  const [busy, setBusy] = useState<'thinking' | 'streaming' | null>(null)
  const listRef = useRef<HTMLDivElement>(null)
  const timers = useRef<number[]>([])
  useEffect(() => () => timers.current.forEach((t) => clearTimeout(t)), [])
  const asked = new Set(msgs.filter((m) => m.role === 'user').map((m) => m.text))
  const suggestions = detail.chat.map((c) => c.q).filter((q) => !asked.has(q))

  useEffect(() => {
    listRef.current?.scrollTo({ top: listRef.current.scrollHeight, behavior: prefersReducedMotion() ? 'auto' : 'smooth' })
  }, [msgs.length, busy])

  const send = (question: string) => {
    const q = question.trim()
    if (!q || busy) return
    setInput('')
    setMsgs((m) => [...m, { role: 'user', text: q, shown: 0 }])
    setBusy('thinking')
    const { text, demoNote } = answerFor(detail, q)
    const total = tokenize(text).length
    const wait = window.setTimeout(() => {
      if (prefersReducedMotion()) {
        setMsgs((m) => [...m, { role: 'assistant', text, shown: total, demoNote }])
        setBusy(null)
        return
      }
      setBusy('streaming')
      setMsgs((m) => [...m, { role: 'assistant', text, shown: 0, demoNote }])
      const timer = window.setInterval(() => {
        setMsgs((m) => {
          const last = m[m.length - 1]
          const shown = Math.min(total, last.shown + 3)
          if (shown >= total) {
            clearInterval(timer)
            setBusy(null)
          }
          return [...m.slice(0, -1), { ...last, shown }]
        })
      }, 28)
      timers.current.push(timer)
    }, 900)
    timers.current.push(wait)
  }

  const onKey = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey && !e.nativeEvent.isComposing) {
      e.preventDefault()
      send(input)
    }
  }

  return (
    <div className="flex min-h-0 flex-1 flex-col">
      <div ref={listRef} className="min-h-0 flex-1 overflow-y-auto px-4 py-4" aria-live="polite" aria-busy={busy !== null}>
        {msgs.length === 0 && (
          <div className="pt-2">
            <p className="text-[13.5px] font-medium">Hỏi về hợp đồng này</p>
            <p className="mt-1 text-[13px] text-ink-2">Mọi câu trả lời đều dẫn về điều khoản cụ thể. Bấm vào trích dẫn để xem trong văn bản.</p>
          </div>
        )}
        <ol className="flex flex-col gap-4">
          {msgs.map((m, i) =>
            m.role === 'user' ? (
              <li key={i} className="enter ml-8 self-end rounded-lg rounded-br-sm bg-primary-soft px-3 py-2 text-[13.5px] text-ink">
                <span className="sr-only">Bạn hỏi: </span>
                {m.text}
              </li>
            ) : (
              <li key={i} className="text-[13.5px] leading-relaxed">
                <span className="sr-only">ContractGuard trả lời: </span>
                <Answer text={tokenize(m.text).slice(0, m.shown).join('')} onJump={onJump} />
                {m.demoNote && m.shown > 0 && (
                  <p className="mt-2 text-[12px] text-ink-3">Bản demo chỉ trả lời được các câu hỏi soạn sẵn. Hãy thử một câu gợi ý bên dưới.</p>
                )}
              </li>
            ),
          )}
          {busy === 'thinking' && (
            <li className="flex flex-col gap-2" aria-label="Đang đọc lại các điều khoản liên quan">
              <span className="text-[12.5px] text-ink-3">Đang đọc lại các điều khoản liên quan…</span>
              <span className="skeleton h-3 w-11/12" />
              <span className="skeleton h-3 w-8/12" />
            </li>
          )}
        </ol>
        {!busy && suggestions.length > 0 && (
          <div className="mt-5 flex flex-col items-start gap-1.5">
            {msgs.length > 0 && <p className="text-[12px] text-ink-3">Có thể bạn muốn hỏi tiếp</p>}
            {suggestions.slice(0, msgs.length ? 3 : 4).map((q) => (
              <button
                key={q}
                type="button"
                onClick={() => send(q)}
                className="rounded-md border border-line bg-paper px-2.5 py-1.5 text-left text-[13px] text-ink-2 transition-colors duration-150 hover:border-line-strong hover:text-ink"
              >
                {q}
              </button>
            ))}
          </div>
        )}
      </div>

      <form
        onSubmit={(e) => {
          e.preventDefault()
          send(input)
        }}
        className="border-t border-line px-3 pt-3 pb-2.5"
      >
        <div className="flex items-end gap-2 rounded-lg border border-line-strong bg-paper px-2.5 py-2 transition-[border-color,box-shadow] duration-150 focus-within:border-primary focus-within:ring-[3px] focus-within:ring-primary/15">
          <label htmlFor="chat-input" className="sr-only">
            Câu hỏi về hợp đồng
          </label>
          <textarea
            id="chat-input"
            rows={1}
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={onKey}
            placeholder="Hỏi về hợp đồng này…"
            className="max-h-28 min-h-6 flex-1 resize-none bg-transparent text-[13.5px] leading-6 placeholder:text-ink-3 focus:outline-none [field-sizing:content]"
          />
          <Button type="submit" variant="primary" size="sm" className="size-8 px-0" aria-label="Gửi câu hỏi" disabled={!input.trim() || busy !== null}>
            <ArrowUp aria-hidden className="size-4" />
          </Button>
        </div>
        <p className="mt-1.5 text-[11.5px] text-ink-3">Trả lời dựa trên nội dung hợp đồng, không phải tư vấn pháp lý.</p>
      </form>
    </div>
  )
}

/** Hiển thị đoạn văn và biến [[9.1]] thành nút nhảy tới điều khoản. */
function Answer({ text, onJump }: { text: string; onJump: (id: string) => void }) {
  return (
    <>
      {text.split('\n\n').map((para, i) => (
        <p key={i} className={cx(i > 0 && 'mt-2.5')}>
          {para.split(/(\[\[[\d.]+\]\])/).map((part, j): ReactNode => {
            const m = part.match(/^\[\[([\d.]+)\]\]$/)
            if (!m) return part
            return (
              <button
                key={j}
                type="button"
                onClick={() => onJump(m[1])}
                className="inline-flex h-5 items-center rounded bg-primary-soft px-1.5 align-[1px] text-[12px] font-medium text-primary-ink transition-colors duration-150 hover:bg-primary/15"
              >
                {clauseLabel(m[1])}
              </button>
            )
          })}
        </p>
      ))}
    </>
  )
}
