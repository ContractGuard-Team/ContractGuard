import { Upload } from 'lucide-react'
import { Link } from 'react-router'
import { SEV_TEXT } from '../../components/risk'
import { buttonClass, cx } from '../../components/ui'
import { SEVERITY_LABEL, formatDateTime, levelOf, type Contract } from '../../model'

export function History({ contract: c }: { contract: Contract }) {
  return (
    <div className="mx-auto grid max-w-[1000px] gap-x-12 gap-y-8 px-4 py-6 sm:px-8 sm:py-8 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
      <section aria-labelledby="ver-title">
        <div className="flex items-center justify-between gap-3">
          <h2 id="ver-title" className="text-[15px] font-semibold">
            Phiên bản
          </h2>
          <Link to="/hop-dong/moi" className={buttonClass('secondary', 'sm')}>
            <Upload aria-hidden className="size-3.5" />
            Tải phiên bản mới
          </Link>
        </div>
        <ol className="mt-3 divide-y divide-line rounded-lg border border-line bg-paper">
          {c.versions.map((v, i) => {
            const older = c.versions[i + 1]?.score
            return (
              <li key={v.v} className="flex gap-4 px-4 py-3.5">
                <span className="flex size-9 shrink-0 items-center justify-center rounded-md bg-canvas text-[13px] font-semibold tabular-nums">V{v.v}</span>
                <div className="min-w-0 flex-1">
                  <p className="text-[13.5px] font-medium">
                    {i === 0 ? 'Phiên bản hiện tại' : 'Phiên bản cũ'}
                    <span className="font-normal text-ink-3"> · {formatDateTime(v.date)}</span>
                  </p>
                  <p className="mt-0.5 text-[12.5px] text-ink-2">
                    {v.by}
                    {v.note && ` · ${v.note}`}
                  </p>
                </div>
                {v.score !== undefined && (
                  <div className="text-right">
                    <p className="text-[15px] font-semibold tabular-nums">{v.score}</p>
                    <p className={cx('text-[12px]', SEV_TEXT[levelOf(v.score)])}>
                      {SEVERITY_LABEL[levelOf(v.score)]}
                      {older !== undefined && <span className="text-ink-3"> ({v.score - older > 0 ? '+' : ''}{v.score - older})</span>}
                    </p>
                  </div>
                )}
              </li>
            )
          })}
        </ol>
        {c.versions.length > 1 && <p className="mt-2 text-[12.5px] text-ink-3">So sánh chi tiết giữa hai phiên bản sẽ có ở bản sau.</p>}
      </section>

      <section aria-labelledby="act-title">
        <h2 id="act-title" className="text-[15px] font-semibold">
          Hoạt động
        </h2>
        <ol className="mt-3 flex flex-col">
          {c.activity.map((a, i) => (
            <li key={i} className="relative pb-4 pl-5 text-[13.5px] before:absolute before:top-[7px] before:left-0 before:size-[7px] before:rounded-full before:bg-line-strong after:absolute after:top-4 after:bottom-0 after:left-[3px] after:w-px after:bg-line last:after:hidden">
              <p>
                <span className="font-medium">{a.by}</span> {a.text}
              </p>
              <p className="text-[12px] text-ink-3">{formatDateTime(a.at)}</p>
            </li>
          ))}
        </ol>
      </section>
    </div>
  )
}
