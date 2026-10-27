import { RotateCcw } from 'lucide-react'
import { DomainBar, RiskRuler, SEV_TEXT, SeverityGlyph, SeverityTag } from '../../components/risk'
import { Button, cx } from '../../components/ui'
import { useStore } from '../../demo/store'
import { DOMAINS, FINDING_STATE_LABEL, SEVERITIES, SEVERITY_LABEL, clauseLabel, levelOf, verdict, type Contract, type Domain, type FindingState } from '../../model'

const SIDE = { buyer: 'Bên mua / khách hàng', seller: 'Bên bán / nhà cung cấp' }
const DATA = { none: 'Không tiếp nhận dữ liệu', normal: 'Dữ liệu kinh doanh thông thường', personal: 'Có dữ liệu cá nhân hoặc nhạy cảm' }
const CRIT = { low: 'Thấp', medium: 'Trung bình', high: 'Cao' }

export function Overview({
  contract: c,
  onOpenFinding,
  onOpenDomain,
}: {
  contract: Contract
  onOpenFinding: (id: string) => void
  onOpenDomain: (d: Domain) => void
}) {
  const { findingEntry, reanalyze } = useStore()
  const r = c.result!
  const level = levelOf(r.score)
  const prev = c.versions[1]?.score
  const findings = c.detail?.findings ?? []
  const urgent = findings.filter((f) => f.severity === 'critical' || f.severity === 'high')
  const stateCounts = findings.reduce<Partial<Record<FindingState, number>>>((acc, f) => {
    const s = findingEntry(c.id, f.id).state
    return { ...acc, [s]: (acc[s] ?? 0) + 1 }
  }, {})

  return (
    <div className="mx-auto grid max-w-[1120px] gap-x-10 gap-y-8 px-4 py-6 sm:px-8 sm:py-8 lg:grid-cols-[minmax(0,1fr)_320px]">
      <div className="flex flex-col gap-9">
        <section aria-labelledby="score-title">
          <h2 id="score-title" className="text-[13.5px] font-semibold text-ink-2">
            Mức rủi ro của hợp đồng
          </h2>
          <div className="mt-2 flex flex-wrap items-baseline gap-x-4 gap-y-1">
            <p className="flex items-baseline gap-1">
              <span className="text-[44px] leading-none font-semibold tracking-[-0.02em] tabular-nums">{r.score}</span>
              <span className="text-[15px] text-ink-3">/100</span>
            </p>
            <SeverityTag severity={level} className="text-[15px]" />
            {prev !== undefined && (
              <span className="text-[13px] text-ink-2">
                {r.score < prev ? `giảm ${prev - r.score}` : `tăng ${r.score - prev}`} điểm so với V{c.versions[1].v}
              </span>
            )}
          </div>
          <p className="mt-3 max-w-[60ch] text-[15px] leading-relaxed">{verdict(r.counts)}</p>
          <div className="max-w-[560px]">
            <RiskRuler score={r.score} compare={prev} compareLabel={prev !== undefined ? `V${c.versions[1].v}` : undefined} />
          </div>
        </section>

        <section aria-labelledby="domain-title">
          <h2 id="domain-title" className="text-[13.5px] font-semibold text-ink-2">
            Theo nhóm rủi ro
          </h2>
          <p className="mt-0.5 text-[12.5px] text-ink-3">Mỗi rủi ro chỉ tính điểm vào một nhóm chính. Nhóm không áp dụng không tính vào điểm tổng.</p>
          <table className="mt-3 w-full text-[13.5px]">
            <caption className="sr-only">Điểm rủi ro theo 7 nhóm</caption>
            <thead className="sr-only">
              <tr>
                <th scope="col">Nhóm</th>
                <th scope="col">Điểm</th>
                <th scope="col">Thang điểm</th>
                <th scope="col">Mức</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-line border-y border-line">
              {DOMAINS.map((d) => {
                const s = r.domainScores[d.id]
                return (
                  <tr key={d.id} className={cx('group', s === null && 'text-ink-3')}>
                    <th scope="row" className="py-2.5 pr-4 text-left font-normal">
                      {s !== null && c.detail ? (
                        <button type="button" onClick={() => onOpenDomain(d.id)} className="rounded text-left font-medium text-ink hover:underline" title={d.question}>
                          {d.label}
                        </button>
                      ) : (
                        <span className={cx(s !== null && 'font-medium text-ink')} title={d.question}>
                          {d.label}
                        </span>
                      )}
                    </th>
                    <td className="w-12 py-2.5 pr-3 text-right font-semibold tabular-nums">{s ?? '—'}</td>
                    <td className="w-[38%] py-2.5 pr-4">
                      <DomainBar score={s} />
                    </td>
                    <td className="w-28 py-2.5 text-[12.5px]">
                      {s === null ? 'Không áp dụng' : <span className={SEV_TEXT[levelOf(s)]}>{SEVERITY_LABEL[levelOf(s)]}</span>}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
          {r.naReason && <p className="mt-2.5 text-[12.5px] text-ink-2">Không áp dụng vì: {r.naReason}</p>}
        </section>

        <p className="max-w-[70ch] text-[12px] text-ink-3">
          Kết quả là rủi ro tiềm ẩn cần xem xét, không phải kết luận pháp lý. ContractGuard hỗ trợ ra quyết định và không thay thế tư vấn của luật sư.
        </p>
      </div>

      <aside className="flex flex-col gap-8">
        <section aria-labelledby="count-title">
          <h2 id="count-title" className="text-[13.5px] font-semibold text-ink-2">
            {r.clauseCount} điều khoản, {Object.values(r.counts).reduce((a, b) => a + b, 0)} rủi ro
          </h2>
          <ul className="mt-2.5 grid grid-cols-2 gap-x-6 gap-y-1.5 text-[13.5px]">
            {SEVERITIES.map((s) => (
              <li key={s} className="flex items-center gap-2">
                <SeverityGlyph severity={s} />
                <span className="flex-1">{SEVERITY_LABEL[s]}</span>
                <span className="font-semibold tabular-nums">{r.counts[s]}</span>
              </li>
            ))}
          </ul>
        </section>

        {urgent.length > 0 && (
          <section aria-labelledby="urgent-title">
            <h2 id="urgent-title" className="text-[13.5px] font-semibold text-ink-2">
              Cần xử lý trước
            </h2>
            <ul className="mt-2 divide-y divide-line border-y border-line">
              {urgent.slice(0, 6).map((f) => {
                const state = findingEntry(c.id, f.id).state
                return (
                  <li key={f.id}>
                    <button type="button" onClick={() => onOpenFinding(f.id)} className="flex w-full gap-2.5 py-2.5 text-left transition-colors duration-150 hover:bg-paper/70">
                      <SeverityGlyph severity={f.severity} className="mt-[5px]" />
                      <span>
                        <span className="block text-[13.5px] leading-snug font-medium">{f.title}</span>
                        <span className="mt-0.5 block text-[12px] text-ink-3">
                          {f.clauseId ? clauseLabel(f.clauseId) : 'Thiếu điều khoản'}
                          {state !== 'open' && ` · ${FINDING_STATE_LABEL[state]}`}
                        </span>
                      </span>
                    </button>
                  </li>
                )
              })}
            </ul>
            {urgent.length > 6 && <p className="mt-2 text-[12.5px] text-ink-3">và {urgent.length - 6} rủi ro cao khác trong tab Văn bản.</p>}
          </section>
        )}

        {findings.length > 0 && (
          <section aria-labelledby="progress-title">
            <h2 id="progress-title" className="text-[13.5px] font-semibold text-ink-2">
              Tiến độ xử lý
            </h2>
            <dl className="mt-2 grid grid-cols-[1fr_auto] gap-y-1 text-[13.5px]">
              {(['open', 'negotiate', 'accepted', 'dismissed'] as FindingState[]).map((s) => (
                <div key={s} className="contents">
                  <dt className="text-ink-2">{FINDING_STATE_LABEL[s]}</dt>
                  <dd className={cx('text-right font-semibold tabular-nums', s === 'open' && (stateCounts.open ?? 0) > 0 && SEV_TEXT.high)}>{stateCounts[s] ?? 0}</dd>
                </div>
              ))}
            </dl>
          </section>
        )}

        <section aria-labelledby="ctx-title" className="rounded-lg border border-line bg-paper px-4 py-3.5">
          <h2 id="ctx-title" className="text-[13.5px] font-semibold">
            Ngữ cảnh đã khai báo
          </h2>
          <dl className="mt-2 flex flex-col gap-1.5 text-[13px]">
            <Pair k="Bên mình">{SIDE[c.context.ourSide]}</Pair>
            {c.context.provides && <Pair k="Đối tác cung cấp">{c.context.provides}</Pair>}
            <Pair k="Dữ liệu">{DATA[c.context.dataAccess]}</Pair>
            <Pair k="Mức quan trọng">{CRIT[c.context.criticality]}</Pair>
          </dl>
          <Button size="sm" className="mt-3" onClick={() => reanalyze(c.id)}>
            <RotateCcw aria-hidden className="size-3.5" />
            Phân tích lại
          </Button>
        </section>
      </aside>
    </div>
  )
}

function Pair({ k, children }: { k: string; children: string }) {
  return (
    <div className="grid grid-cols-[112px_1fr] gap-2">
      <dt className="text-ink-3">{k}</dt>
      <dd>{children}</dd>
    </div>
  )
}
