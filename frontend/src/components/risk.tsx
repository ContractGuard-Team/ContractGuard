import { AlertTriangle, Check, LoaderCircle, Minus, X } from 'lucide-react'
import { CHECK_STATUS_LABEL, SEVERITY_LABEL, STATUS_LABEL, levelOf, type CheckStatus, type ReviewStatus, type Severity } from '../model'
import { cx } from './ui'

export const SEV_TEXT: Record<Severity, string> = { critical: 'text-critical', high: 'text-high', medium: 'text-medium', low: 'text-low' }
export const SEV_TINT: Record<Severity, string> = {
  critical: 'bg-critical-tint',
  high: 'bg-high-tint',
  medium: 'bg-medium-tint',
  low: 'bg-low-tint',
}
const SEV_GLYPH: Record<Severity, string> = { critical: 'text-critical', high: 'text-high', medium: 'text-medium-mark', low: 'text-low' }
const SEV_FILL: Record<Severity, string> = { critical: 'bg-critical', high: 'bg-high', medium: 'bg-medium-mark', low: 'bg-low' }

/** Ký hiệu hình học cho severity: màu không bao giờ là tín hiệu duy nhất. */
export function SeverityGlyph({ severity, className }: { severity: Severity; className?: string }) {
  return (
    <svg viewBox="0 0 12 12" aria-hidden className={cx('size-3 shrink-0', SEV_GLYPH[severity], className)}>
      {severity === 'critical' && <path d="M6 .6 11.4 6 6 11.4.6 6Z" fill="currentColor" />}
      {severity === 'high' && <path d="M6 1.2 11.2 10.4H.8Z" fill="currentColor" />}
      {severity === 'medium' && <circle cx="6" cy="6" r="4.5" fill="currentColor" />}
      {severity === 'low' && <circle cx="6" cy="6" r="4" fill="none" stroke="currentColor" strokeWidth="1.7" />}
    </svg>
  )
}

export function SeverityTag({ severity, className }: { severity: Severity; className?: string }) {
  return (
    <span className={cx('inline-flex items-center gap-1.5 font-medium', SEV_TEXT[severity], className)}>
      <SeverityGlyph severity={severity} />
      {SEVERITY_LABEL[severity]}
    </span>
  )
}

const ZONES: { from: number; to: number; sev: Severity }[] = [
  { from: 0, to: 30, sev: 'low' },
  { from: 30, to: 55, sev: 'medium' },
  { from: 55, to: 85, sev: 'high' },
  { from: 85, to: 100, sev: 'critical' },
]

/** Thước 0–100 với vạch ngưỡng theo spec 7.6, thay cho donut gauge. */
export function RiskRuler({ score, compare, compareLabel }: { score: number; compare?: number; compareLabel?: string }) {
  const level = levelOf(score)
  return (
    <div className="pt-6">
      <div className="relative">
        <div className="flex h-2 gap-px overflow-hidden rounded-full">
          {ZONES.map((z) => (
            <div
              key={z.sev}
              className={cx(z.sev === level ? SEV_FILL[z.sev] : SEV_TINT[z.sev], 'transition-colors')}
              style={{ width: `${z.to - z.from}%` }}
            />
          ))}
        </div>
        {compare !== undefined && (
          <div className="absolute -top-1.5 -translate-x-1/2" style={{ left: `${compare}%` }}>
            <div className="h-5 w-0 border-l border-dashed border-ink-3" />
            <span className="absolute -top-5 left-1/2 -translate-x-1/2 text-[11.5px] whitespace-nowrap text-ink-3 tabular-nums">
              {compareLabel} {compare}
            </span>
          </div>
        )}
        <div className="absolute -top-1.5 -translate-x-1/2" style={{ left: `${score}%` }}>
          <div className="h-5 w-[3px] rounded-full bg-ink ring-2 ring-paper" />
        </div>
      </div>
      <div className="relative mt-2 h-4 text-[11.5px] text-ink-3">
        {ZONES.map((z, i) => (
          <span
            key={z.sev}
            className={cx('absolute whitespace-nowrap', i === ZONES.length - 1 && '-translate-x-full', z.sev === level && 'font-medium text-ink')}
            style={{ left: `${i === ZONES.length - 1 ? 100 : z.from}%` }}
          >
            {SEVERITY_LABEL[z.sev]}
          </span>
        ))}
      </div>
    </div>
  )
}

/** Thanh điểm cho một nhóm rủi ro; null = không áp dụng. */
export function DomainBar({ score }: { score: number | null }) {
  if (score === null) return <div className="h-1.5 rounded-full border border-dashed border-line-strong" />
  return (
    <div className="relative h-1.5 rounded-full bg-line">
      <div className={cx('absolute inset-y-0 left-0 rounded-full', SEV_FILL[levelOf(score)])} style={{ width: `${score}%` }} />
      {[30, 55, 85].map((t) => (
        <span key={t} className="absolute inset-y-0 w-px bg-paper" style={{ left: `${t}%` }} />
      ))}
    </div>
  )
}

export function StatusBadge({ status, className }: { status: ReviewStatus; className?: string }) {
  const Icon = { analyzing: LoaderCircle, failed: AlertTriangle, approved: Check, rejected: X, review: null, negotiating: null }[status]
  return (
    <span
      className={cx(
        'inline-flex h-6 items-center gap-1 rounded-full px-2 text-[12.5px] font-medium whitespace-nowrap',
        status === 'review' && 'bg-primary-soft text-primary-ink',
        status === 'failed' && 'bg-critical-tint text-critical',
        !['review', 'failed'].includes(status) && 'bg-ink/[0.06] text-ink-2',
        className,
      )}
    >
      {Icon && <Icon aria-hidden className={cx('size-3.5', status === 'analyzing' && 'animate-spin')} />}
      {STATUS_LABEL[status]}
    </span>
  )
}

export function CheckStatusTag({ status }: { status: CheckStatus }) {
  return (
    <span
      className={cx(
        'inline-flex items-center gap-1.5 font-medium whitespace-nowrap',
        status === 'pass' && 'text-low',
        status === 'partial' && 'text-medium',
        status === 'missing' && 'text-high',
        status === 'na' && 'text-ink-3',
      )}
    >
      {status === 'pass' && <Check aria-hidden className="size-3.5" strokeWidth={2.5} />}
      {status === 'partial' && (
        <svg viewBox="0 0 14 14" aria-hidden className="size-3.5">
          <circle cx="7" cy="7" r="5.5" fill="none" stroke="currentColor" strokeWidth="1.6" />
          <path d="M7 1.5a5.5 5.5 0 0 1 0 11Z" fill="currentColor" />
        </svg>
      )}
      {status === 'missing' && <X aria-hidden className="size-3.5" strokeWidth={2.5} />}
      {status === 'na' && <Minus aria-hidden className="size-3.5" />}
      {CHECK_STATUS_LABEL[status]}
    </span>
  )
}
