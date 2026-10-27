import type { ReactNode } from 'react'
import { SeverityGlyph } from '../../components/risk'
import { cx } from '../../components/ui'
import { FINDING_STATE_LABEL, type ContractDetail, type Finding, type FindingState, type Severity } from '../../model'

const MARK: Record<Severity, string> = {
  critical: 'bg-critical-tint decoration-critical',
  high: 'bg-high-tint decoration-high',
  medium: 'bg-medium-tint decoration-medium-mark',
  low: 'bg-low-tint decoration-low',
}
const MARK_SELECTED: Record<Severity, string> = {
  critical: 'bg-critical/20',
  high: 'bg-high/20',
  medium: 'bg-medium-mark/30',
  low: 'bg-low/20',
}

/** Một hàng: phần văn bản nằm trên giấy, phần ghi chú nằm ở lề (xuống dưới văn bản khi màn hình hẹp). */
function Row({ children, aside }: { children: ReactNode; aside?: ReactNode }) {
  return (
    <div className="flex flex-col xl:flex-row">
      <div className="px-6 sm:px-12 xl:w-[var(--paper-w)] xl:shrink-0 xl:px-14">{children}</div>
      {aside && <div className="px-5 font-sans sm:px-11 xl:w-[244px] xl:shrink-0 xl:px-0 xl:pl-8">{aside}</div>}
    </div>
  )
}

interface Props {
  detail: ContractDetail
  selectedId: string | null
  onSelect: (findingId: string) => void
  stateOf: (findingId: string) => FindingState
}

/** Văn bản hợp đồng dạng trang giấy; rủi ro là ghi chú lề thẳng hàng với từng khoản. */
export function DocumentView({ detail, selectedId, onSelect, stateOf }: Props) {
  const pageCount = Math.max(...detail.articles.map((a) => a.page))
  const byClause = new Map<string, Finding[]>()
  for (const f of detail.findings) if (f.clauseId) byClause.set(f.clauseId, [...(byClause.get(f.clauseId) ?? []), f])
  const missing = detail.findings.filter((f) => f.kind === 'missing')
  const done = (id: string) => ['accepted', 'dismissed', 'resolved'].includes(stateOf(id))

  const notes = (list: Finding[] = []) =>
    list.length > 0 && (
      <ul className="flex flex-col gap-0.5 pb-2 xl:pt-0.5 xl:pb-0">
        {list.map((f) => {
          const selected = f.id === selectedId
          return (
            <li key={f.id} className="relative xl:before:absolute xl:before:top-[13px] xl:before:-left-7 xl:before:w-5 xl:before:border-t xl:before:border-dashed xl:before:border-line-strong">
              <button
                type="button"
                aria-pressed={selected}
                onClick={() => onSelect(f.id)}
                className={cx(
                  'flex w-full items-start gap-2 rounded-md px-2 py-1.5 text-left text-[12.5px] leading-snug transition-[background-color,color,box-shadow] duration-150',
                  selected ? 'bg-paper text-ink shadow-[0_0_0_1px_var(--color-line-strong)] xl:bg-paper' : 'text-ink-2 hover:bg-canvas hover:text-ink xl:hover:bg-paper/70',
                  done(f.id) && !selected && 'opacity-60',
                )}
              >
                <SeverityGlyph severity={f.severity} className="mt-[3px]" />
                <span>
                  <span className="sr-only">{f.id}, </span>
                  {f.title}
                  {stateOf(f.id) !== 'open' && <span className="mt-0.5 block text-[11.5px] text-ink-3">{FINDING_STATE_LABEL[stateOf(f.id)]}</span>}
                </span>
              </button>
            </li>
          )
        })}
      </ul>
    )

  const highlight = (text: string, list: Finding[] = []) => {
    const ranges = list
      .filter((f) => f.quote)
      .map((f) => ({ f, start: text.indexOf(f.quote!) }))
      .filter((r) => r.start >= 0)
      .sort((a, b) => a.start - b.start)
    const out: ReactNode[] = []
    let pos = 0
    for (const { f, start } of ranges) {
      if (start < pos) continue
      out.push(text.slice(pos, start))
      out.push(
        <mark
          key={f.id}
          onClick={() => onSelect(f.id)}
          className={cx(
            'cursor-pointer rounded-[2px] text-inherit underline underline-offset-[5px] transition-[background-color] duration-150',
            done(f.id) ? 'bg-transparent decoration-ink-3/50 decoration-dotted' : MARK[f.severity],
            f.id === selectedId ? cx('decoration-[3px]', MARK_SELECTED[f.severity]) : 'decoration-2',
          )}
        >
          {f.quote}
        </mark>,
      )
      pos = start + f.quote!.length
    }
    out.push(text.slice(pos))
    return out
  }

  return (
    <div className="flex flex-col gap-6 px-3 py-6 sm:px-6 xl:py-8">
      {Array.from({ length: pageCount }, (_, i) => i + 1).map((page) => {
        const articles = detail.articles.filter((a) => a.page === page)
        return (
          <section
            key={page}
            aria-label={`Trang ${page} trên ${pageCount}`}
            className="relative mx-auto w-full max-w-[var(--paper-w)] xl:w-[calc(var(--paper-w)+244px)] xl:max-w-none"
          >
            <div
              aria-hidden
              className="absolute inset-y-0 left-0 w-full rounded-[3px] bg-paper shadow-[0_1px_2px_oklch(0.2_0.02_220/0.06),0_10px_28px_-14px_oklch(0.2_0.02_220/0.22)] xl:w-[var(--paper-w)]"
            />
            <div className="relative pt-10 pb-6 font-serif text-[16px] leading-[1.75] sm:pt-14">
              {page === 1 && (
                <Row>
                  <div className="text-center">
                    <p className="text-[14px] font-semibold tracking-[0.02em]">CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM</p>
                    <p className="text-[14px] font-semibold underline underline-offset-4">Độc lập – Tự do – Hạnh phúc</p>
                    <h2 className="mt-8 text-[18px] font-bold">{detail.heading}</h2>
                    <p className="mt-1 text-[14px] text-ink-2">{detail.number}</p>
                  </div>
                  <p className="mt-8">{detail.preamble}</p>
                  {[detail.partyA, detail.partyB].map((p) => (
                    <div key={p.label} className="mt-3">
                      <p className="font-semibold">
                        {p.label}: {p.name}
                      </p>
                      <p className="text-ink-2">Đại diện: {p.rep}</p>
                    </div>
                  ))}
                  <p className="mt-3 mb-2">Hai Bên thống nhất ký kết Hợp đồng với các điều khoản sau:</p>
                </Row>
              )}

              {articles.map((a, ai) => (
                <div key={a.number}>
                  <Row>
                    <h3 className={cx('mb-1.5 text-[16px] font-semibold', ai === 0 && page !== 1 ? 'mt-0' : 'mt-6')}>
                      Điều {a.number}. {a.title}
                    </h3>
                  </Row>
                  {a.clauses.map((cl) => (
                    <Row key={cl.id} aside={notes(byClause.get(cl.id))}>
                      <p id={`clause-${cl.id}`} className="-mx-2 mb-2 scroll-mt-24 rounded-sm px-2">
                        {a.clauses.length > 1 && <span className="text-ink-2 tabular-nums">{cl.id}. </span>}
                        {highlight(cl.text, byClause.get(cl.id))}
                      </p>
                    </Row>
                  ))}
                </div>
              ))}

              {page === pageCount && (
                <>
                  {missing.length > 0 && (
                    <Row aside={notes(missing)}>
                      <div className="mt-6 rounded-md border border-dashed border-line-strong px-4 py-3 font-sans text-[13px] leading-relaxed text-ink-2">
                        <p className="font-medium text-ink">Không tìm thấy trong hợp đồng</p>
                        <p className="mt-0.5">
                          Hệ thống đã tìm nhưng không thấy điều khoản nào về {missing.map((f) => f.missingWhat).join('; ')}.
                        </p>
                      </div>
                    </Row>
                  )}
                  <Row>
                    <div className="mt-10 grid grid-cols-2 gap-6 text-center text-[14px]">
                      {[
                        ['ĐẠI DIỆN BÊN A', detail.partyA.rep],
                        ['ĐẠI DIỆN BÊN B', detail.partyB.rep],
                      ].map(([t, rep]) => (
                        <div key={t}>
                          <p className="font-semibold">{t}</p>
                          <p className="text-[13px] text-ink-3 italic">(Ký, ghi rõ họ tên)</p>
                          <p className="mt-14 font-semibold">{rep.split(',')[0].replace(/^(Ông|Bà) /, '')}</p>
                        </div>
                      ))}
                    </div>
                  </Row>
                </>
              )}

              <Row>
                <p className="mt-8 text-right font-sans text-[11.5px] text-ink-3 tabular-nums">
                  Trang {page}/{pageCount}
                </p>
              </Row>
            </div>
          </section>
        )
      })}
    </div>
  )
}
