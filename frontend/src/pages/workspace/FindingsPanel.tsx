import { ArrowLeft, ChevronLeft, ChevronRight } from 'lucide-react'
import { useEffect, useRef, useState, type ReactNode } from 'react'
import { SeverityGlyph, SeverityTag } from '../../components/risk'
import { Button, Select, Textarea, cx } from '../../components/ui'
import { useStore } from '../../demo/store'
import {
  DOMAINS,
  DOMAIN_LABEL,
  FINDING_STATE_LABEL,
  SEVERITIES,
  SEVERITY_LABEL,
  clauseLabel,
  type Domain,
  type Finding,
  type FindingState,
  type Severity,
} from '../../model'

const EDITABLE_STATES: FindingState[] = ['open', 'negotiate', 'accepted', 'dismissed']

interface Props {
  contractId: string
  findings: Finding[]
  selectedId: string | null
  onSelect: (id: string | null) => void
  /** Cuộn văn bản tới điều khoản, giữ nguyên bảng đang mở. */
  onJump: (clauseId: string) => void
  /** Cuộn tới điều khoản và nhường màn hình cho văn bản (đóng ngăn kéo khi màn hình hẹp). */
  onShowClause: (clauseId: string) => void
  domain: Domain | ''
  onDomain: (d: Domain | '') => void
}

export function FindingsPanel({ contractId, findings, selectedId, onSelect, onJump, onShowClause, domain, onDomain }: Props) {
  const { findingEntry } = useStore()
  const [sev, setSev] = useState<Severity | ''>('')
  const [hideDone, setHideDone] = useState(false)
  const selected = findings.find((f) => f.id === selectedId)

  if (selected)
    return (
      <FindingDetail key={selected.id} contractId={contractId} finding={selected} findings={findings} onSelect={onSelect} onJump={onJump} onShowClause={onShowClause} />
    )

  const isDone = (f: Finding) => findingEntry(contractId, f.id).state !== 'open' && findingEntry(contractId, f.id).state !== 'negotiate'
  const visible = findings.filter((f) => (!sev || f.severity === sev) && (!domain || f.domain === domain || f.secondary?.includes(domain)) && (!hideDone || !isDone(f)))
  const counts = Object.fromEntries(SEVERITIES.map((s) => [s, findings.filter((f) => f.severity === s).length])) as Record<Severity, number>

  return (
    <div className="flex min-h-0 flex-1 flex-col">
      <div className="flex flex-col gap-2.5 border-b border-line px-4 pt-3 pb-3">
        <div role="group" aria-label="Lọc theo mức độ" className="flex flex-wrap gap-1">
          <FilterChip active={!sev} onClick={() => setSev('')}>
            Tất cả <span className="tabular-nums text-ink-3">{findings.length}</span>
          </FilterChip>
          {SEVERITIES.filter((s) => counts[s] > 0).map((s) => (
            <FilterChip key={s} active={sev === s} onClick={() => setSev(sev === s ? '' : s)} label={`${SEVERITY_LABEL[s]}: ${counts[s]}`}>
              <SeverityGlyph severity={s} />
              <span className="tabular-nums">{counts[s]}</span>
            </FilterChip>
          ))}
        </div>
        <div className="flex items-center gap-3">
          <Select aria-label="Lọc theo nhóm rủi ro" value={domain} onChange={(e) => onDomain(e.target.value as Domain | '')} className="min-w-0 flex-1 [&_select]:h-8 [&_select]:text-[13px]">
            <option value="">Mọi nhóm rủi ro</option>
            {DOMAINS.map((d) => (
              <option key={d.id} value={d.id}>
                {d.label}
              </option>
            ))}
          </Select>
          <label className="flex shrink-0 items-center gap-1.5 text-[12.5px] text-ink-2">
            <input type="checkbox" checked={hideDone} onChange={(e) => setHideDone(e.target.checked)} className="size-3.5 accent-[var(--color-primary)]" />
            Ẩn mục đã xử lý
          </label>
        </div>
      </div>

      <div className="min-h-0 flex-1 overflow-y-auto">
        {visible.length === 0 ? (
          <p className="px-4 py-10 text-center text-[13px] text-ink-3">Không có rủi ro nào khớp bộ lọc.</p>
        ) : (
          SEVERITIES.map((s) => {
            const group = visible.filter((f) => f.severity === s)
            if (!group.length) return null
            return (
              <section key={s} aria-label={SEVERITY_LABEL[s]}>
                <h3 className="sticky top-0 border-b border-line bg-paper px-4 py-1.5 text-[12px] font-medium text-ink-3">
                  {SEVERITY_LABEL[s]} · <span className="tabular-nums">{group.length}</span>
                </h3>
                <ul>
                  {group.map((f) => {
                    const state = findingEntry(contractId, f.id).state
                    return (
                      <li key={f.id} className="border-b border-line last:border-b-0">
                        <button
                          type="button"
                          onClick={() => {
                            onSelect(f.id)
                            if (f.clauseId) onJump(f.clauseId)
                          }}
                          className="flex w-full gap-2.5 px-4 py-3 text-left transition-colors duration-150 hover:bg-canvas"
                        >
                          <SeverityGlyph severity={f.severity} className="mt-1" />
                          <span className="min-w-0">
                            <span className={cx('block text-[13.5px] leading-snug font-medium', isDone(f) && 'text-ink-2')}>{f.title}</span>
                            <span className="mt-1 block text-[12px] text-ink-3">
                              {f.kind === 'missing' ? 'Thiếu điều khoản' : clauseLabel(f.clauseId!)} · {DOMAIN_LABEL[f.domain]}
                              {state !== 'open' && <span className={cx('ml-1.5', state === 'negotiate' ? 'text-primary-ink' : 'text-ink-2')}>· {FINDING_STATE_LABEL[state]}</span>}
                            </span>
                          </span>
                        </button>
                      </li>
                    )
                  })}
                </ul>
              </section>
            )
          })
        )}
      </div>
    </div>
  )
}

function FilterChip({ active, onClick, children, label }: { active: boolean; onClick: () => void; children: ReactNode; label?: string }) {
  return (
    <button
      type="button"
      aria-pressed={active}
      aria-label={label}
      onClick={onClick}
      className={cx(
        'inline-flex h-7 items-center gap-1.5 rounded-full border px-2.5 text-[12.5px] font-medium transition-colors duration-150',
        active ? 'border-ink bg-ink text-white [&_.text-ink-3]:text-white/70' : 'border-line-strong bg-paper text-ink-2 hover:border-ink-3/60 hover:text-ink',
      )}
    >
      {children}
    </button>
  )
}

function FindingDetail({
  contractId,
  finding: f,
  findings,
  onSelect,
  onJump,
  onShowClause,
}: {
  contractId: string
  finding: Finding
  findings: Finding[]
  onSelect: (id: string | null) => void
  onJump: (clauseId: string) => void
  onShowClause: (clauseId: string) => void
}) {
  const { findingEntry, setFinding, toast } = useStore()
  const entry = findingEntry(contractId, f.id)
  const [note, setNote] = useState(entry.note)
  const headingRef = useRef<HTMLHeadingElement>(null)
  const i = findings.findIndex((x) => x.id === f.id)
  const go = (d: number) => {
    const next = findings[i + d]
    if (!next) return
    onSelect(next.id)
    if (next.clauseId) onJump(next.clauseId)
  }

  useEffect(() => headingRef.current?.focus(), [])

  return (
    <div className="flex min-h-0 flex-1 flex-col">
      <div className="flex items-center gap-1 border-b border-line px-2 py-1.5">
        <Button variant="ghost" size="sm" onClick={() => onSelect(null)}>
          <ArrowLeft aria-hidden className="size-4" />
          Tất cả rủi ro
        </Button>
        <span className="ml-auto text-[12px] text-ink-3 tabular-nums">
          {i + 1}/{findings.length}
        </span>
        <Button variant="ghost" size="sm" className="size-8 px-0" aria-label="Rủi ro trước" disabled={i === 0} onClick={() => go(-1)}>
          <ChevronLeft aria-hidden className="size-4" />
        </Button>
        <Button variant="ghost" size="sm" className="size-8 px-0" aria-label="Rủi ro sau" disabled={i === findings.length - 1} onClick={() => go(1)}>
          <ChevronRight aria-hidden className="size-4" />
        </Button>
      </div>

      <article className="enter min-h-0 flex-1 overflow-y-auto px-5 pt-4 pb-6">
        <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-[12.5px]">
          <SeverityTag severity={f.severity} />
          <span className="text-ink-3">
            {DOMAIN_LABEL[f.domain]}
            {f.secondary?.length ? `, liên quan ${f.secondary.map((d) => DOMAIN_LABEL[d].toLowerCase()).join(', ')}` : ''}
          </span>
          <span className="ml-auto text-ink-3 tabular-nums">{f.id}</span>
        </div>
        <h2 ref={headingRef} tabIndex={-1} className="mt-2 text-[17px] leading-snug font-semibold focus:outline-none">
          {f.title}
        </h2>

        {f.kind === 'issue' ? (
          <figure className="mt-4 rounded-md bg-canvas px-4 py-3">
            <blockquote className="font-serif text-[15px] leading-relaxed">“{f.quote}”</blockquote>
            <figcaption className="mt-2">
              <button type="button" onClick={() => onShowClause(f.clauseId!)} className="text-[12.5px] font-medium text-primary hover:underline">
                Xem {clauseLabel(f.clauseId!)} trong văn bản
              </button>
            </figcaption>
          </figure>
        ) : (
          <p className="mt-4 rounded-md border border-dashed border-line-strong px-4 py-3 text-[13px] text-ink-2">
            Hợp đồng không có điều khoản nào về {f.missingWhat}.
          </p>
        )}

        <dl className="mt-5 flex flex-col gap-4 text-[13.5px] leading-relaxed">
          <Section title="Vì sao đây là rủi ro">{f.why}</Section>
          <Section title="Tác động có thể xảy ra">{f.impact}</Section>
          <Section title="Hướng xử lý gợi ý">{f.recommendation}</Section>
          {f.reference && <Section title="Căn cứ tham khảo">{f.reference}</Section>}
        </dl>

        <div className="mt-6 border-t border-line pt-5">
          <fieldset>
            <legend className="text-[13px] font-semibold">Xử lý</legend>
            <div className="mt-2 grid grid-cols-2 gap-1.5">
              {EDITABLE_STATES.map((s) => (
                <label
                  key={s}
                  className={cx(
                    'flex cursor-pointer items-center gap-2 rounded-md border px-2.5 py-2 text-[13px] transition-colors duration-150',
                    entry.state === s ? 'border-primary bg-primary-soft/60 font-medium text-ink' : 'border-line text-ink-2 hover:border-line-strong',
                  )}
                >
                  <input
                    type="radio"
                    name={`state-${f.id}`}
                    checked={entry.state === s}
                    onChange={() => {
                      setFinding(contractId, f.id, { state: s })
                      toast(`Đã đánh dấu ${f.id}: ${FINDING_STATE_LABEL[s]}`)
                    }}
                    className="size-3.5 accent-[var(--color-primary)]"
                  />
                  {FINDING_STATE_LABEL[s]}
                </label>
              ))}
            </div>
          </fieldset>
          <label htmlFor={`note-${f.id}`} className="mt-4 block text-[13px] font-medium">
            Ghi chú nội bộ
          </label>
          <Textarea
            id={`note-${f.id}`}
            rows={3}
            value={note}
            placeholder="Ví dụ: đã hỏi lại đối tác ngày 25/10"
            onChange={(e) => setNote(e.target.value)}
            onBlur={() => {
              if (note !== entry.note) {
                setFinding(contractId, f.id, { note })
                toast('Đã lưu ghi chú')
              }
            }}
            className="mt-1.5"
          />
          <p className="mt-1.5 text-[12px] text-ink-3">Chỉ người trong tổ chức được chia sẻ hợp đồng này mới xem được.</p>
        </div>
      </article>
    </div>
  )
}

function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div>
      <dt className="text-[12.5px] font-semibold text-ink-2">{title}</dt>
      <dd className="mt-1 text-ink">{children}</dd>
    </div>
  )
}
