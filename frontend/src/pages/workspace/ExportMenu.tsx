import { ChevronDown, Download } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router'
import { Button, cx } from '../../components/ui'
import type { Contract } from '../../model'

/** Popover chọn định dạng và phạm vi báo cáo (EXP-01..03), rồi mở bản xem trước. */
export function ExportMenu({ contract: c }: { contract: Contract }) {
  const [open, setOpen] = useState(false)
  const [format, setFormat] = useState<'pdf' | 'docx'>('pdf')
  const [scope, setScope] = useState<'all' | 'top'>('all')
  const ref = useRef<HTMLDivElement>(null)
  const navigate = useNavigate()
  const counts = c.result?.counts
  const total = counts ? counts.critical + counts.high + counts.medium + counts.low : 0
  const top = counts ? counts.critical + counts.high : 0

  useEffect(() => {
    if (!open) return
    const onDown = (e: MouseEvent) => !ref.current?.contains(e.target as Node) && setOpen(false)
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && setOpen(false)
    document.addEventListener('mousedown', onDown)
    document.addEventListener('keydown', onKey)
    ref.current?.querySelector<HTMLInputElement>('input:checked')?.focus()
    return () => {
      document.removeEventListener('mousedown', onDown)
      document.removeEventListener('keydown', onKey)
    }
  }, [open])

  const radio = (name: string, checked: boolean, onChange: () => void, label: string, desc: string) => (
    <label className={cx('flex cursor-pointer gap-2.5 rounded-md px-2.5 py-2 transition-colors duration-150', checked ? 'bg-primary-soft/60' : 'hover:bg-canvas')}>
      <input type="radio" name={name} checked={checked} onChange={onChange} className="mt-0.5 size-3.5 accent-[var(--color-primary)]" />
      <span>
        <span className="block text-[13px] font-medium">{label}</span>
        <span className="block text-[12px] text-ink-3">{desc}</span>
      </span>
    </label>
  )

  return (
    <div ref={ref} className="relative">
      <Button aria-expanded={open} aria-haspopup="dialog" onClick={() => setOpen((o) => !o)} disabled={!c.result}>
        <Download aria-hidden className="size-4" />
        <span className="max-sm:sr-only">Xuất báo cáo</span>
        <ChevronDown aria-hidden className="size-3.5 text-ink-3" />
      </Button>
      {open && (
        <div
          role="dialog"
          aria-label="Xuất báo cáo"
          className="enter absolute top-full right-0 mt-2 w-[300px] rounded-lg border border-line bg-paper p-3 shadow-xl"
          style={{ zIndex: 'var(--z-popover)' }}
        >
          <fieldset>
            <legend className="px-2.5 text-[12.5px] font-semibold text-ink-2">Định dạng</legend>
            <div className="mt-1 flex flex-col">
              {radio('fmt', format === 'pdf', () => setFormat('pdf'), 'PDF', 'Để gửi đi hoặc in')}
              {radio('fmt', format === 'docx', () => setFormat('docx'), 'Word', 'Để chỉnh sửa tiếp trước khi gửi đối tác')}
            </div>
          </fieldset>
          <fieldset className="mt-3">
            <legend className="px-2.5 text-[12.5px] font-semibold text-ink-2">Phạm vi</legend>
            <div className="mt-1 flex flex-col">
              {radio('scope', scope === 'all', () => setScope('all'), `Toàn bộ ${total} rủi ro`, 'Kèm checklist bảo mật')}
              {radio('scope', scope === 'top', () => setScope('top'), `Chỉ Nghiêm trọng và Cao (${top})`, 'Bản ngắn cho người duyệt')}
            </div>
          </fieldset>
          <Button variant="primary" className="mt-3 w-full" onClick={() => navigate(`/hop-dong/${c.id}/bao-cao?dinh-dang=${format}&pham-vi=${scope}`)}>
            Xem trước và tải xuống
          </Button>
        </div>
      )}
    </div>
  )
}
