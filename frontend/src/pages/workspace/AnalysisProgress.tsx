import { AlertTriangle, Check, LoaderCircle } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { buttonClass, cx } from '../../components/ui'
import { ANALYSIS_STEPS, ANALYSIS_STEP_MS, useStore } from '../../demo/store'
import type { Contract } from '../../model'

export function AnalysisProgress({ contract: c }: { contract: Contract }) {
  // shortcut: bước hiện tại tính theo đồng hồ giả lập; thay bằng trạng thái job thật (poll GET /jobs/{id}) khi có backend.
  const { analysisStart, completeAnalysis } = useStore()
  const start = analysisStart[c.id] ?? Date.now()
  const [now, setNow] = useState(() => Date.now())
  const total = ANALYSIS_STEPS.length * ANALYSIS_STEP_MS
  const elapsed = now - start
  const step = Math.min(ANALYSIS_STEPS.length - 1, Math.floor(elapsed / ANALYSIS_STEP_MS))

  useEffect(() => {
    if (elapsed >= total) {
      completeAnalysis(c.id)
      return
    }
    const t = setTimeout(() => setNow(Date.now()), 200)
    return () => clearTimeout(t)
  }, [now, elapsed, total, c.id, completeAnalysis])

  return (
    <div className="mx-auto w-full max-w-[520px] px-4 py-12 sm:py-16">
      <h2 className="text-[19px] font-semibold">Đang phân tích hợp đồng</h2>
      <p className="mt-1 text-[13.5px] text-ink-2">Bạn có thể rời trang này. Kết quả được lưu lại khi phân tích xong.</p>

      <div className="mt-6 h-1.5 overflow-hidden rounded-full bg-line" role="progressbar" aria-label="Tiến độ phân tích" aria-valuemin={0} aria-valuemax={100} aria-valuenow={Math.min(100, Math.round((elapsed / total) * 100))}>
        <div className="h-full rounded-full bg-primary transition-[width] duration-200 ease-linear" style={{ width: `${Math.min(100, (elapsed / total) * 100)}%` }} />
      </div>

      <ol className="mt-6 flex flex-col gap-3.5" aria-live="polite">
        {ANALYSIS_STEPS.map((label, i) => {
          const state = i < step ? 'done' : i === step ? 'running' : 'pending'
          return (
            <li key={label} className={cx('flex items-center gap-3 text-[14px]', state === 'pending' && 'text-ink-3')} aria-current={state === 'running' ? 'step' : undefined}>
              <span className={cx('flex size-6 items-center justify-center rounded-full', state === 'done' ? 'bg-primary text-white' : 'border border-line-strong bg-paper')}>
                {state === 'done' && <Check aria-hidden className="size-3.5" strokeWidth={3} />}
                {state === 'running' && <LoaderCircle aria-hidden className="size-3.5 animate-spin text-primary" />}
              </span>
              <span className={cx(state === 'running' && 'font-medium')}>
                {label}
                {i === 1 && state === 'done' && <span className="text-ink-3"> · đã tách {c.result?.clauseCount ?? 26} điều khoản</span>}
              </span>
              <span className="sr-only">{state === 'done' ? 'xong' : state === 'running' ? 'đang chạy' : 'chờ'}</span>
            </li>
          )
        })}
      </ol>
    </div>
  )
}

export function AnalysisFailed({ contract: c }: { contract: Contract }) {
  return (
    <div className="mx-auto w-full max-w-[520px] px-4 py-12 sm:py-16">
      <AlertTriangle aria-hidden className="size-6 text-critical" />
      <h2 className="mt-3 text-[19px] font-semibold">Không đọc được tệp hợp đồng</h2>
      <p className="mt-1.5 text-[14px] leading-relaxed text-ink-2">{c.error}</p>
      <p className="mt-3 text-[13.5px] text-ink-2">Hãy mở khóa tệp hoặc xuất lại thành PDF không đặt mật khẩu, rồi tải lên lại. Chưa có dữ liệu nào được lưu từ lần phân tích này.</p>
      <Link to="/hop-dong/moi" className={cx(buttonClass('primary'), 'mt-6')}>
        Tải lên tệp khác
      </Link>
    </div>
  )
}
