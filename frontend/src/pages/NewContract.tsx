import { Check, FileText, Minus, Upload } from 'lucide-react'
import { useRef, useState, type DragEvent, type FormEvent, type ReactNode } from 'react'
import { Link, useNavigate } from 'react-router'
import { AppShell } from '../components/AppShell'
import { Button, Field, Input, Select, buttonClass, cx } from '../components/ui'
import { useStore } from '../demo/store'
import { DOMAINS, type ContractContext, type Domain } from '../model'

const ACCEPT = ['pdf', 'docx', 'jpg', 'jpeg', 'png']
const MAX_MB = 20
const TYPES = ['Dịch vụ', 'Mua bán', 'Hợp đồng khung', 'SOW', 'Thuê', 'Khác']

type Ctx = Partial<ContractContext>

/** Nhóm nào áp dụng theo ngữ cảnh (spec 7.1, quy tắc 2). */
function applicable(ctx: Ctx, type: string): Record<Domain, boolean> {
  const data = ctx.dataAccess === 'normal' || ctx.dataAccess === 'personal'
  const service = type !== 'Mua bán'
  return {
    legal: true,
    financial: true,
    operational: true,
    compliance: true,
    cyber: data,
    privacy: data,
    thirdparty: data || service,
  }
}

const sizeLabel = (b: number) => `${(b / 1024 / 1024).toLocaleString('vi-VN', { maximumFractionDigits: 1 })} MB`

export default function NewContract() {
  const { createContract } = useStore()
  const navigate = useNavigate()
  const fileRef = useRef<HTMLInputElement>(null)
  const [file, setFile] = useState<File | null>(null)
  const [fileError, setFileError] = useState('')
  const [reading, setReading] = useState(false)
  const [autofilled, setAutofilled] = useState(false)
  const [meta, setMeta] = useState({ title: '', partner: '', type: 'Dịch vụ', value: '' })
  const [ctx, setCtx] = useState<Ctx>({ criticality: 'medium' })
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [dragging, setDragging] = useState(false)
  const clear = (key: string) => setErrors((e) => ({ ...e, [key]: '' }))

  const pick = (f: File | undefined) => {
    if (!f) return
    const ext = f.name.split('.').pop()?.toLowerCase() ?? ''
    if (!ACCEPT.includes(ext)) return setFileError('Chỉ nhận tệp PDF, DOCX hoặc ảnh JPG/PNG.')
    if (f.size > MAX_MB * 1024 * 1024) return setFileError(`Tệp lớn hơn ${MAX_MB} MB. Hãy nén hoặc tách bớt phụ lục rồi thử lại.`)
    setFileError('')
    clear('file')
    setFile(f)
    setReading(true)
    // shortcut: giả lập bước đọc nhanh metadata; khi có backend thì điền từ kết quả trích xuất thật.
    setTimeout(() => {
      setReading(false)
      setAutofilled(true)
      setMeta({
        title: 'Hợp đồng cung cấp phần mềm kế toán trên nền tảng đám mây',
        partner: 'Công ty TNHH Phần mềm Sổ Xanh',
        type: 'Dịch vụ',
        value: '132.000.000 ₫/năm',
      })
      setCtx((c) => ({ ...c, provides: c.provides || 'Phần mềm kế toán trên cloud' }))
    }, 1200)
  }

  const onDrop = (e: DragEvent) => {
    e.preventDefault()
    setDragging(false)
    pick(e.dataTransfer.files[0])
  }

  const onSubmit = (e: FormEvent) => {
    e.preventDefault()
    const next: Record<string, string> = {}
    if (!file) next.file = 'Chọn tệp hợp đồng cần phân tích.'
    if (!meta.title.trim()) next.title = 'Nhập tên hợp đồng.'
    if (!meta.partner.trim()) next.partner = 'Nhập tên đối tác.'
    if (!ctx.ourSide) next.ourSide = 'Chọn bên mình trong hợp đồng để hệ thống đánh giá đúng góc nhìn.'
    if (!ctx.dataAccess) next.dataAccess = 'Chọn một mục để hệ thống biết có cần đánh giá nhóm bảo mật và dữ liệu không.'
    setErrors(next)
    if (Object.keys(next).length) {
      document.querySelector<HTMLElement>('[aria-invalid="true"], [data-error="true"]')?.focus()
      return
    }
    const id = createContract({
      title: meta.title.trim(),
      partner: meta.partner.trim(),
      type: meta.type,
      value: meta.value.trim() || undefined,
      fileName: file!.name,
      context: { ourSide: ctx.ourSide!, provides: ctx.provides ?? '', dataAccess: ctx.dataAccess!, criticality: ctx.criticality ?? 'medium' },
    })
    navigate(`/hop-dong/${id}`)
  }

  const apply = applicable(ctx, meta.type)

  return (
    <AppShell>
      <div className="mx-auto max-w-[1100px] px-4 py-6 sm:px-8 sm:py-8">
        <nav aria-label="Breadcrumb" className="text-[13px] text-ink-3">
          <Link to="/hop-dong" className="hover:text-ink hover:underline">
            Hợp đồng
          </Link>{' '}
          / Tải lên
        </nav>
        <h1 className="mt-1 text-[22px] font-semibold tracking-[-0.01em]">Tải hợp đồng lên</h1>
        <p className="mt-1 max-w-[62ch] text-[13.5px] text-ink-2">Hệ thống đọc hợp đồng, tách từng điều khoản và đánh giá rủi ro từ góc nhìn của bên mình.</p>

        <form onSubmit={onSubmit} noValidate className="mt-7 grid gap-8 lg:grid-cols-[minmax(0,1fr)_300px]">
          <div className="flex flex-col gap-9">
            <Step n={1} title="Tệp hợp đồng">
              <input
                ref={fileRef}
                id="contract-file"
                type="file"
                accept={ACCEPT.map((x) => `.${x}`).join(',')}
                className="peer sr-only"
                aria-label="Chọn tệp hợp đồng"
                data-error={errors.file ? 'true' : undefined}
                onChange={(e) => {
                  pick(e.target.files?.[0])
                  e.target.value = ''
                }}
              />
              {file ? (
                <div className="flex items-center gap-3 rounded-lg border border-line bg-paper px-4 py-3">
                  <FileText aria-hidden className="size-5 shrink-0 text-primary" />
                  <div className="min-w-0 flex-1">
                    <p className="truncate font-medium">{file.name}</p>
                    <p className="text-[12.5px] text-ink-3">{reading ? 'Đang đọc thông tin cơ bản…' : `${sizeLabel(file.size)} · đã kiểm tra định dạng`}</p>
                  </div>
                  <Button size="sm" variant="ghost" onClick={() => fileRef.current?.click()}>
                    Đổi tệp
                  </Button>
                </div>
              ) : (
                <label
                  htmlFor="contract-file"
                  onDragOver={(e) => {
                    e.preventDefault()
                    setDragging(true)
                  }}
                  onDragLeave={() => setDragging(false)}
                  onDrop={onDrop}
                  className={cx(
                    'flex cursor-pointer flex-col items-center rounded-lg border border-dashed px-6 py-10 text-center transition-colors duration-150',
                    'peer-focus-visible:ring-2 peer-focus-visible:ring-primary hover:border-primary/60 hover:bg-primary-soft/40',
                    dragging ? 'border-primary bg-primary-soft/60' : errors.file || fileError ? 'border-critical bg-paper' : 'border-line-strong bg-paper',
                  )}
                >
                  <Upload aria-hidden className="size-6 text-ink-3" />
                  <span className="mt-3 font-medium">Kéo thả tệp vào đây, hoặc bấm để chọn</span>
                  <span className="mt-1 text-[12.5px] text-ink-3">PDF, DOCX hoặc ảnh chụp/scan (JPG, PNG), tối đa {MAX_MB} MB</span>
                </label>
              )}
              {(fileError || errors.file) && (
                <p role="alert" className="mt-2 text-[12.5px] text-critical">
                  {fileError || errors.file}
                </p>
              )}
            </Step>

            <Step
              n={2}
              title="Thông tin hợp đồng"
              aside={autofilled && <span className="rounded bg-primary-soft px-1.5 py-0.5 text-[12px] font-medium text-primary-ink">Tự điền từ tệp, hãy kiểm tra lại</span>}
            >
              <div className="grid gap-4 sm:grid-cols-2">
                <Field label="Tên hợp đồng" error={errors.title} className="sm:col-span-2">
                  {reading ? <div className="skeleton h-9" /> : <Input value={meta.title} onChange={(e) => (setMeta({ ...meta, title: e.target.value }), clear('title'))} />}
                </Field>
                <Field label="Đối tác" error={errors.partner}>
                  {reading ? <div className="skeleton h-9" /> : <Input value={meta.partner} onChange={(e) => (setMeta({ ...meta, partner: e.target.value }), clear('partner'))} />}
                </Field>
                <Field label="Loại hợp đồng">
                  <Select value={meta.type} onChange={(e) => setMeta({ ...meta, type: e.target.value })}>
                    {TYPES.map((t) => (
                      <option key={t}>{t}</option>
                    ))}
                  </Select>
                </Field>
                <Field label="Giá trị" hint="Không bắt buộc.">
                  {reading ? <div className="skeleton h-9" /> : <Input value={meta.value} onChange={(e) => setMeta({ ...meta, value: e.target.value })} />}
                </Field>
              </div>
            </Step>

            <Step n={3} title="Ngữ cảnh">
              <div className="flex flex-col gap-6">
                <Choice
                  name="ourSide"
                  legend="Bên mình là bên nào trong hợp đồng?"
                  hint="Cùng một điều khoản có thể là rủi ro cho bên mua nhưng có lợi cho bên bán."
                  error={errors.ourSide}
                  value={ctx.ourSide}
                  onChange={(v) => (setCtx({ ...ctx, ourSide: v as ContractContext['ourSide'] }), clear('ourSide'))}
                  columns
                  options={[
                    { value: 'buyer', label: 'Bên mua / khách hàng', desc: 'Mình trả tiền để nhận hàng hóa hoặc dịch vụ.' },
                    { value: 'seller', label: 'Bên bán / nhà cung cấp', desc: 'Mình giao hàng hóa hoặc cung cấp dịch vụ.' },
                  ]}
                />
                <Field label="Đối tác cung cấp gì cho mình?" hint="Giúp đánh giá mức độ quan trọng, ví dụ: phần mềm kế toán trên cloud.">
                  <Input value={ctx.provides ?? ''} onChange={(e) => setCtx({ ...ctx, provides: e.target.value })} />
                </Field>
                <Choice
                  name="dataAccess"
                  legend="Đối tác có tiếp nhận hoặc xử lý dữ liệu của mình không?"
                  error={errors.dataAccess}
                  value={ctx.dataAccess}
                  onChange={(v) => (setCtx({ ...ctx, dataAccess: v as ContractContext['dataAccess'] }), clear('dataAccess'))}
                  options={[
                    { value: 'none', label: 'Không', desc: 'Ví dụ: mua bàn ghế, thuê bảo vệ.' },
                    { value: 'normal', label: 'Có, dữ liệu kinh doanh thông thường', desc: 'Ví dụ: đơn hàng, báo cáo bán hàng.' },
                    { value: 'personal', label: 'Có, gồm dữ liệu cá nhân hoặc nhạy cảm', desc: 'Ví dụ: hồ sơ nhân viên, bảng lương, thông tin khách hàng.' },
                  ]}
                />
                <Choice
                  name="criticality"
                  legend="Dịch vụ hoặc hàng hóa này quan trọng thế nào với công việc của mình?"
                  value={ctx.criticality}
                  onChange={(v) => setCtx({ ...ctx, criticality: v as ContractContext['criticality'] })}
                  inline
                  options={[
                    { value: 'low', label: 'Thấp' },
                    { value: 'medium', label: 'Trung bình' },
                    { value: 'high', label: 'Cao, ngừng là ảnh hưởng ngay' },
                  ]}
                />
              </div>
            </Step>

            <div className="flex flex-wrap items-center gap-3 border-t border-line pt-6">
              <Button type="submit" variant="primary" disabled={reading}>
                Bắt đầu phân tích
              </Button>
              <Link to="/hop-dong" className={buttonClass('ghost')}>
                Hủy
              </Link>
              <p className="text-[12.5px] text-ink-3">Thường mất 30–60 giây với hợp đồng khoảng 20 trang.</p>
            </div>
          </div>

          <aside aria-labelledby="scope-title" className="self-start lg:sticky lg:top-8">
            <h2 id="scope-title" className="text-[13.5px] font-semibold">
              Hệ thống sẽ đánh giá
            </h2>
            <p className="mt-1 text-[12.5px] text-ink-3">Cập nhật theo câu trả lời ở phần Ngữ cảnh.</p>
            <ul className="mt-3 divide-y divide-line rounded-lg border border-line bg-paper">
              {DOMAINS.map((d) => (
                <li key={d.id} className="flex items-center gap-2.5 px-3.5 py-2.5 text-[13px]">
                  {apply[d.id] ? <Check aria-hidden className="size-4 text-primary" /> : <Minus aria-hidden className="size-4 text-ink-3" />}
                  <span className={cx('flex-1', !apply[d.id] && 'text-ink-3')}>{d.label}</span>
                  {!apply[d.id] && <span className="text-[12px] text-ink-3">Không áp dụng</span>}
                </li>
              ))}
            </ul>
            {(!apply.cyber || !apply.thirdparty) && (
              <p className="mt-3 text-[12.5px] text-ink-2">
                Nhóm không áp dụng sẽ không tính vào điểm tổng. Nếu hợp đồng thực ra có điều khoản về dữ liệu, hệ thống sẽ tự bật lại và báo cho bạn.
              </p>
            )}
          </aside>
        </form>
      </div>
    </AppShell>
  )
}

function Step({ n, title, aside, children }: { n: number; title: string; aside?: ReactNode; children: ReactNode }) {
  return (
    <section aria-labelledby={`step-${n}`}>
      <div className="mb-3 flex flex-wrap items-center gap-2.5">
        <span aria-hidden className="inline-flex size-6 items-center justify-center rounded-full border border-line-strong bg-paper text-[12px] font-semibold text-ink-2 tabular-nums">
          {n}
        </span>
        <h2 id={`step-${n}`} className="text-[15px] font-semibold">
          {title}
        </h2>
        {aside}
      </div>
      {children}
    </section>
  )
}

function Choice({
  name,
  legend,
  hint,
  error,
  value,
  onChange,
  options,
  columns,
  inline,
}: {
  name: string
  legend: string
  hint?: string
  error?: string
  value?: string
  onChange: (v: string) => void
  options: { value: string; label: string; desc?: string }[]
  columns?: boolean
  inline?: boolean
}) {
  return (
    <fieldset aria-describedby={error ? `${name}-err` : hint ? `${name}-hint` : undefined}>
      <legend className="text-[13px] font-medium">{legend}</legend>
      {hint && (
        <p id={`${name}-hint`} className="mt-1 text-[12.5px] text-ink-3">
          {hint}
        </p>
      )}
      <div className={cx('mt-2.5 grid gap-2', columns && 'sm:grid-cols-2', inline && 'sm:flex sm:flex-wrap')}>
        {options.map((o, i) => (
          <label
            key={o.value}
            className={cx(
              'flex cursor-pointer gap-2.5 rounded-lg border bg-paper px-3.5 py-3 transition-colors duration-150',
              value === o.value ? 'border-primary bg-primary-soft/50' : error ? 'border-critical/60' : 'border-line hover:border-line-strong',
              inline && 'items-center py-2.5',
            )}
          >
            <input
              type="radio"
              name={name}
              value={o.value}
              checked={value === o.value}
              onChange={() => onChange(o.value)}
              data-error={error && i === 0 ? 'true' : undefined}
              className="mt-0.5 size-4 shrink-0 accent-[var(--color-primary)]"
            />
            <span>
              <span className="block text-[13.5px] font-medium">{o.label}</span>
              {o.desc && <span className="mt-0.5 block text-[12.5px] text-ink-3">{o.desc}</span>}
            </span>
          </label>
        ))}
      </div>
      {error && (
        <p id={`${name}-err`} className="mt-2 text-[12.5px] text-critical">
          {error}
        </p>
      )}
    </fieldset>
  )
}
