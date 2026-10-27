import { ChevronDown } from 'lucide-react'
import {
  cloneElement,
  useId,
  type ButtonHTMLAttributes,
  type InputHTMLAttributes,
  type KeyboardEvent,
  type ReactElement,
  type ReactNode,
  type SelectHTMLAttributes,
  type TextareaHTMLAttributes,
} from 'react'
import { initials } from '../model'

export const cx = (...c: (string | false | null | undefined)[]) => c.filter(Boolean).join(' ')

type Variant = 'primary' | 'secondary' | 'ghost' | 'danger'
type Size = 'sm' | 'md'

export function buttonClass(variant: Variant = 'secondary', size: Size = 'md') {
  return cx(
    'inline-flex shrink-0 items-center justify-center gap-1.5 rounded-md font-medium whitespace-nowrap select-none',
    'transition-[background-color,border-color,color] duration-150 ease-out',
    'disabled:cursor-not-allowed disabled:opacity-50 aria-disabled:pointer-events-none aria-disabled:opacity-50',
    size === 'md' ? 'h-9 px-3.5 text-[13.5px]' : 'h-8 px-2.5 text-[13px]',
    variant === 'primary' && 'bg-primary text-white hover:bg-primary-hover active:bg-primary-ink',
    variant === 'secondary' && 'border border-line-strong bg-paper text-ink hover:border-ink-3/50 hover:bg-canvas active:bg-line/60',
    variant === 'ghost' && 'text-ink-2 hover:bg-ink/[0.06] hover:text-ink active:bg-ink/10',
    variant === 'danger' && 'text-critical hover:bg-critical-tint active:bg-critical-tint',
  )
}

export function Button({
  variant,
  size,
  className,
  type = 'button',
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & { variant?: Variant; size?: Size }) {
  return <button type={type} className={cx(buttonClass(variant, size), className)} {...props} />
}

const control =
  'w-full rounded-md border border-line-strong bg-paper text-ink placeholder:text-ink-3 transition-[border-color,box-shadow] duration-150 hover:border-ink-3/60 focus:border-primary focus:outline-none focus:ring-[3px] focus:ring-primary/15 aria-invalid:border-critical aria-invalid:focus:ring-critical/15 disabled:bg-canvas disabled:text-ink-3'

export function Input({ className, ...props }: InputHTMLAttributes<HTMLInputElement>) {
  return <input className={cx(control, 'h-9 px-3 text-[13.5px]', className)} {...props} />
}

export function Textarea({ className, ...props }: TextareaHTMLAttributes<HTMLTextAreaElement>) {
  return <textarea className={cx(control, 'px-3 py-2 text-[13.5px] leading-relaxed', className)} {...props} />
}

export function Select({ className, children, ...props }: SelectHTMLAttributes<HTMLSelectElement>) {
  return (
    <div className={cx('relative', className)}>
      <select className={cx(control, 'h-9 appearance-none pr-8 pl-3 text-[13.5px]')} {...props}>
        {children}
      </select>
      <ChevronDown aria-hidden className="pointer-events-none absolute top-1/2 right-2.5 size-4 -translate-y-1/2 text-ink-3" />
    </div>
  )
}

/** Gắn label, gợi ý và lỗi vào đúng control con qua id/aria. */
export function Field({
  label,
  hint,
  error,
  children,
  className,
}: {
  label: ReactNode
  hint?: ReactNode
  error?: string
  children: ReactElement<{ id?: string; 'aria-describedby'?: string; 'aria-invalid'?: boolean }>
  className?: string
}) {
  const id = useId()
  const describedBy = [hint && `${id}-hint`, error && `${id}-err`].filter(Boolean).join(' ') || undefined
  return (
    <div className={cx('flex flex-col gap-1.5', className)}>
      <label htmlFor={id} className="text-[13px] font-medium text-ink">
        {label}
      </label>
      {cloneElement(children, { id, 'aria-describedby': describedBy, 'aria-invalid': error ? true : undefined })}
      {hint && !error && (
        <p id={`${id}-hint`} className="text-[12.5px] text-ink-3">
          {hint}
        </p>
      )}
      {error && (
        <p id={`${id}-err`} className="text-[12.5px] text-critical">
          {error}
        </p>
      )}
    </div>
  )
}

/** Tab theo chuẩn WAI-ARIA: phím mũi tên chuyển tab, Tab đi vào nội dung. */
export function TabList<T extends string>({
  tabs,
  value,
  onChange,
  label,
  idPrefix,
  className,
}: {
  tabs: { id: T; label: ReactNode }[]
  value: T
  onChange: (id: T) => void
  label: string
  idPrefix: string
  className?: string
}) {
  const onKey = (e: KeyboardEvent<HTMLDivElement>) => {
    const i = tabs.findIndex((t) => t.id === value)
    const next = e.key === 'ArrowRight' ? i + 1 : e.key === 'ArrowLeft' ? i - 1 : e.key === 'Home' ? 0 : e.key === 'End' ? tabs.length - 1 : null
    if (next === null) return
    e.preventDefault()
    const t = tabs[(next + tabs.length) % tabs.length]
    onChange(t.id)
    document.getElementById(`${idPrefix}-tab-${t.id}`)?.focus()
  }
  return (
    <div role="tablist" aria-label={label} onKeyDown={onKey} className={cx('flex gap-1', className)}>
      {tabs.map((t) => {
        const active = t.id === value
        return (
          <button
            key={t.id}
            id={`${idPrefix}-tab-${t.id}`}
            role="tab"
            type="button"
            aria-selected={active}
            aria-controls={`${idPrefix}-panel-${t.id}`}
            tabIndex={active ? 0 : -1}
            onClick={() => onChange(t.id)}
            className={cx(
              'relative inline-flex h-10 shrink-0 items-center gap-1.5 px-2.5 text-[13.5px] font-medium whitespace-nowrap transition-colors duration-150',
              'after:absolute after:inset-x-2.5 after:bottom-0 after:h-0.5 after:rounded-full after:transition-colors after:duration-150',
              active ? 'text-ink after:bg-primary' : 'text-ink-3 hover:text-ink after:bg-transparent',
            )}
          >
            {t.label}
          </button>
        )
      })}
    </div>
  )
}

export function Avatar({ name, size = 28, className }: { name: string; size?: number; className?: string }) {
  // Màu lấy từ tên để mỗi người cố định một màu; giữ độ đậm đủ cho chữ trắng.
  const hue = [...name].reduce((h, ch) => (h * 31 + ch.charCodeAt(0)) % 360, 7)
  return (
    <span
      aria-hidden
      className={cx('inline-flex shrink-0 items-center justify-center rounded-full font-semibold text-white', className)}
      style={{ width: size, height: size, fontSize: size * 0.4, background: `oklch(0.48 0.06 ${hue})` }}
    >
      {initials(name)}
    </span>
  )
}

export function Toasts({ toasts }: { toasts: { id: number; text: string }[] }) {
  return (
    <div
      role="status"
      aria-live="polite"
      className="pointer-events-none fixed right-4 bottom-4 flex flex-col items-end gap-2"
      style={{ zIndex: 'var(--z-toast)' }}
    >
      {toasts.map((t) => (
        <div key={t.id} className="enter rounded-md bg-ink px-3.5 py-2.5 text-[13px] text-white shadow-lg">
          {t.text}
        </div>
      ))}
    </div>
  )
}
