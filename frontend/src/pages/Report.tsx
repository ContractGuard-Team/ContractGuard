import { ArrowLeft, Download } from 'lucide-react'
import { useRef } from 'react'
import { Link, useParams, useSearchParams } from 'react-router'
import { EmptyState, Wordmark } from '../components/AppShell'
import { CheckStatusTag, SEV_TEXT, SeverityGlyph, SeverityTag } from '../components/risk'
import { Button, buttonClass, cx } from '../components/ui'
import { useStore } from '../demo/store'
import { DOMAINS, DOMAIN_LABEL, FINDING_STATE_LABEL, SEVERITIES, SEVERITY_LABEL, clauseLabel, formatDate, levelOf, verdict } from '../model'

export default function Report() {
  const { id = '' } = useParams()
  const [params] = useSearchParams()
  const { getContract, findingEntry, me } = useStore()
  const c = getContract(id)
  const format = params.get('dinh-dang') === 'docx' ? 'docx' : 'pdf'
  const scope = params.get('pham-vi') === 'top' ? 'top' : 'all'
  const docRef = useRef<HTMLElement>(null)

  if (!c?.result)
    return (
      <EmptyState
        title="Chưa có báo cáo cho hợp đồng này"
        action={
          <Link to="/hop-dong" className={buttonClass('secondary')}>
            Về thư viện
          </Link>
        }
      />
    )

  const r = c.result
  const findings = (c.detail?.findings ?? []).filter((f) => scope === 'all' || f.severity === 'critical' || f.severity === 'high')
  const today = formatDate(new Date().toISOString())
  const fileBase = `BaoCaoRuiRo-${c.id}`

  const download = () => {
    if (format === 'pdf') return window.print()
    // shortcut: Word mở được HTML lưu dưới đuôi .doc; FE thật nhận tệp .docx do backend sinh.
    const html = `<html><head><meta charset="utf-8"><title>${c.title}</title></head><body style="font-family:'Be Vietnam Pro',Calibri,sans-serif;font-size:11pt">${docRef.current?.innerHTML ?? ''}</body></html>`
    const url = URL.createObjectURL(new Blob(['﻿', html], { type: 'application/msword' }))
    const a = Object.assign(document.createElement('a'), { href: url, download: `${fileBase}.doc` })
    a.click()
    URL.revokeObjectURL(url)
  }

  return (
    <div className="min-h-dvh pb-16 print:bg-white print:pb-0">
      <div className="sticky top-0 border-b border-line bg-sidebar print:hidden" style={{ zIndex: 'var(--z-sticky)' }}>
        <div className="mx-auto flex max-w-[860px] flex-wrap items-center gap-3 px-4 py-2.5">
          <Link to={`/hop-dong/${c.id}?tab=tong-quan`} className={buttonClass('ghost', 'sm')}>
            <ArrowLeft aria-hidden className="size-4" />
            Quay lại hợp đồng
          </Link>
          <p className="mr-auto text-[13px] text-ink-2">
            Xem trước báo cáo {format === 'pdf' ? 'PDF' : 'Word'} · {scope === 'all' ? 'toàn bộ rủi ro' : 'chỉ Nghiêm trọng và Cao'}
          </p>
          <Link
            to={`?dinh-dang=${format}&pham-vi=${scope === 'all' ? 'top' : 'all'}`}
            replace
            className={buttonClass('secondary', 'sm')}
          >
            {scope === 'all' ? 'Chuyển sang bản ngắn' : 'Chuyển sang toàn bộ rủi ro'}
          </Link>
          <Button variant="primary" size="sm" onClick={download}>
            <Download aria-hidden className="size-4" />
            {format === 'pdf' ? 'Tải PDF' : 'Tải bản Word'}
          </Button>
        </div>
      </div>

      <main className="mx-auto mt-8 max-w-[820px] px-4 print:mt-0 print:max-w-none print:px-0">
        <article
          ref={docRef}
          className="rounded-[3px] bg-paper px-6 py-10 text-[13.5px] leading-relaxed shadow-[0_1px_2px_oklch(0.2_0.02_220/0.06),0_10px_28px_-14px_oklch(0.2_0.02_220/0.22)] sm:px-14 sm:py-14 print:rounded-none print:px-0 print:py-0 print:shadow-none"
        >
          <header className="flex items-start justify-between gap-4 border-b border-line pb-5">
            <div>
              <Wordmark />
              <p className="mt-3 text-[12.5px] text-ink-3">Báo cáo rủi ro hợp đồng · lập ngày {today} bởi {me.name}</p>
            </div>
            <p className="text-right text-[12.5px] text-ink-3">Bảo mật nội bộ</p>
          </header>

          <h1 className="mt-6 text-[22px] leading-snug font-semibold">{c.title}</h1>
          <dl className="mt-3 grid grid-cols-[140px_1fr] gap-x-4 gap-y-1 text-[13px]">
            {[
              ['Đối tác', c.partner],
              ['Loại hợp đồng', c.type],
              ['Giá trị', c.value ?? 'Không ghi'],
              ['Phiên bản', `V${c.versions[0].v}, ${formatDate(c.versions[0].date)}`],
              ['Góc nhìn đánh giá', c.context.ourSide === 'buyer' ? 'Bên mua / khách hàng' : 'Bên bán / nhà cung cấp'],
            ].map(([k, v]) => (
              <div key={k} className="contents">
                <dt className="text-ink-3">{k}</dt>
                <dd>{v}</dd>
              </div>
            ))}
          </dl>

          <h2 className="mt-8 text-[15px] font-semibold">Kết luận</h2>
          <p className="mt-2 flex flex-wrap items-baseline gap-x-3">
            <span className="text-[28px] leading-none font-semibold tabular-nums">{r.score}/100</span>
            <SeverityTag severity={levelOf(r.score)} />
          </p>
          <p className="mt-2">{verdict(r.counts)}</p>
          <p className="mt-1 text-ink-2">
            {r.clauseCount} điều khoản được phân tích. Rủi ro: {SEVERITIES.map((s) => `${r.counts[s]} ${SEVERITY_LABEL[s].toLowerCase()}`).join(', ')}.
          </p>

          <h2 className="mt-8 text-[15px] font-semibold">Điểm theo nhóm rủi ro</h2>
          <table className="mt-2 w-full text-[13px]">
            <tbody className="divide-y divide-line border-y border-line">
              {DOMAINS.map((d) => {
                const s = r.domainScores[d.id]
                return (
                  <tr key={d.id}>
                    <th scope="row" className="py-1.5 text-left font-normal">
                      {d.label}
                    </th>
                    <td className="w-16 py-1.5 text-right font-semibold tabular-nums">{s ?? '—'}</td>
                    <td className={cx('w-32 py-1.5 pl-4', s === null ? 'text-ink-3' : SEV_TEXT[levelOf(s)])}>{s === null ? 'Không áp dụng' : SEVERITY_LABEL[levelOf(s)]}</td>
                  </tr>
                )
              })}
            </tbody>
          </table>

          <h2 className="mt-8 text-[15px] font-semibold">
            Chi tiết rủi ro {scope === 'top' && <span className="font-normal text-ink-2">(chỉ Nghiêm trọng và Cao)</span>}
          </h2>
          {findings.length === 0 ? (
            <p className="mt-2 text-ink-2">
              {c.detail ? 'Không có rủi ro nào trong phạm vi đã chọn.' : 'Bản demo chưa có danh sách rủi ro chi tiết cho hợp đồng này.'}
            </p>
          ) : (
            <ol className="mt-3 flex flex-col gap-5">
              {findings.map((f) => (
                <li key={f.id} className="break-inside-avoid border-t border-line pt-4">
                  <p className="flex flex-wrap items-center gap-x-3 text-[12.5px] text-ink-3">
                    <span className="tabular-nums">{f.id}</span>
                    <SeverityTag severity={f.severity} />
                    <span>{DOMAIN_LABEL[f.domain]}</span>
                    <span>{f.clauseId ? clauseLabel(f.clauseId) : 'Thiếu điều khoản'}</span>
                    <span>Xử lý: {FINDING_STATE_LABEL[findingEntry(c.id, f.id).state]}</span>
                  </p>
                  <h3 className="mt-1 text-[14.5px] font-semibold">{f.title}</h3>
                  {f.quote && <blockquote className="mt-2 border-l border-line-strong pl-3 font-serif text-[14px] text-ink-2 italic">“{f.quote}”</blockquote>}
                  <p className="mt-2">
                    <strong className="font-semibold">Vì sao: </strong>
                    {f.why}
                  </p>
                  <p className="mt-1">
                    <strong className="font-semibold">Tác động: </strong>
                    {f.impact}
                  </p>
                  <p className="mt-1">
                    <strong className="font-semibold">Hướng xử lý: </strong>
                    {f.recommendation}
                  </p>
                  {f.reference && <p className="mt-1 text-[12.5px] text-ink-3">Căn cứ tham khảo: {f.reference}</p>}
                </li>
              ))}
            </ol>
          )}

          {scope === 'all' && c.detail && c.detail.checks.length > 0 && (
            <>
              <h2 className="mt-8 text-[15px] font-semibold">Checklist bảo mật</h2>
              <table className="mt-2 w-full text-[13px]">
                <tbody className="divide-y divide-line border-y border-line">
                  {c.detail.checks.map((x) => (
                    <tr key={x.id}>
                      <td className="w-16 py-1.5 text-ink-3 tabular-nums">{x.id}</td>
                      <td className="py-1.5">{x.name}</td>
                      <td className="w-36 py-1.5 text-right">
                        <CheckStatusTag status={x.status} />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </>
          )}

          <footer className="mt-10 border-t border-line pt-4 text-[12px] text-ink-3">
            <p className="flex items-center gap-1.5">
              <SeverityGlyph severity="critical" /> Nghiêm trọng <SeverityGlyph severity="high" className="ml-2" /> Cao <SeverityGlyph severity="medium" className="ml-2" /> Trung
              bình <SeverityGlyph severity="low" className="ml-2" /> Thấp
            </p>
            <p className="mt-2">
              Báo cáo nêu các rủi ro tiềm ẩn cần xem xét, không phải kết luận pháp lý. ContractGuard hỗ trợ ra quyết định và không thay thế tư vấn của luật sư.
            </p>
          </footer>
        </article>
      </main>
    </div>
  )
}
