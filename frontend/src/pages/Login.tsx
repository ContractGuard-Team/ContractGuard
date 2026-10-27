import { useState, type FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { Wordmark } from '../components/AppShell'
import { Button, Field, Input, TabList } from '../components/ui'
import { ORG_NAME } from '../demo/mock'

type Mode = 'dang-nhap' | 'dang-ky'

export default function Login() {
  const [params, setParams] = useSearchParams()
  const invited = params.has('loi-moi')
  const mode: Mode = invited || params.get('che-do') === 'dang-ky' ? 'dang-ky' : 'dang-nhap'
  const navigate = useNavigate()
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [submitting, setSubmitting] = useState(false)
  const [resetSent, setResetSent] = useState(false)

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const data = new FormData(e.currentTarget)
    const next: Record<string, string> = {}
    const email = String(data.get('email') ?? '').trim()
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) next.email = 'Nhập email hợp lệ, ví dụ ten@congty.vn'
    if (String(data.get('password') ?? '').length < 8) next.password = 'Mật khẩu cần ít nhất 8 ký tự'
    if (mode === 'dang-ky' && !String(data.get('name') ?? '').trim()) next.name = 'Nhập họ và tên'
    setErrors(next)
    if (Object.keys(next).length) return
    setSubmitting(true)
    // shortcut: demo không xác thực thật, chuyển thẳng vào app. Thay bằng Cognito (OIDC) khi có backend.
    setTimeout(() => navigate('/hop-dong'), 500)
  }

  return (
    <div className="flex min-h-dvh flex-col items-center px-4 py-10 sm:py-16">
      <Wordmark />
      <p className="mt-3 max-w-[38ch] text-center text-[13.5px] text-ink-2">
        Đọc hợp đồng đối tác gửi sang, chỉ ra điều khoản rủi ro và giải thích vì sao.
      </p>

      <main className="mt-8 w-full max-w-[400px] rounded-[3px] bg-paper px-6 pt-5 pb-7 shadow-[0_1px_2px_oklch(0.2_0.02_220/0.06),0_12px_32px_-16px_oklch(0.2_0.02_220/0.25)] sm:px-8">
        {invited ? (
          <div className="mb-5 rounded-md bg-primary-soft px-3.5 py-3 text-[13px] text-primary-ink">
            <strong className="font-semibold">Trần Quốc Bảo</strong> mời bạn tham gia <strong className="font-semibold">{ORG_NAME}</strong>. Tạo tài khoản bằng
            email được mời để bắt đầu.
          </div>
        ) : (
          <TabList
            label="Đăng nhập hoặc tạo tài khoản"
            idPrefix="auth"
            className="-mx-2.5 mb-5 border-b border-line"
            value={mode}
            onChange={(m) => {
              setErrors({})
              setParams(m === 'dang-ky' ? { 'che-do': 'dang-ky' } : {})
            }}
            tabs={[
              { id: 'dang-nhap', label: 'Đăng nhập' },
              { id: 'dang-ky', label: 'Tạo tài khoản' },
            ]}
          />
        )}

        <form id={`auth-panel-${mode}`} role={invited ? undefined : 'tabpanel'} aria-labelledby={invited ? undefined : `auth-tab-${mode}`} onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
          {mode === 'dang-ky' && (
            <Field label="Họ và tên" error={errors.name}>
              <Input name="name" autoComplete="name" />
            </Field>
          )}
          <Field label="Email công việc" error={errors.email}>
            <Input name="email" type="email" autoComplete="email" defaultValue={invited ? 'lam.nguyen@lamsonviet.vn' : undefined} readOnly={invited} />
          </Field>
          <Field label="Mật khẩu" hint={mode === 'dang-ky' ? 'Ít nhất 8 ký tự.' : undefined} error={errors.password}>
            <Input name="password" type="password" autoComplete={mode === 'dang-ky' ? 'new-password' : 'current-password'} />
          </Field>
          {mode === 'dang-nhap' &&
            (resetSent ? (
              <p role="status" className="-mt-2 text-[12.5px] text-ink-2">
                Nếu email đã có tài khoản, link đặt lại mật khẩu sẽ được gửi tới hộp thư trong vài phút.
              </p>
            ) : (
              <button type="button" onClick={() => setResetSent(true)} className="-mt-2 self-end rounded text-[12.5px] font-medium text-primary hover:underline">
                Quên mật khẩu?
              </button>
            ))}
          <Button type="submit" variant="primary" disabled={submitting} className="mt-1 w-full">
            {submitting ? 'Đang vào…' : mode === 'dang-ky' ? 'Tạo tài khoản' : 'Đăng nhập'}
          </Button>
        </form>

        {!invited && (
          <>
            <div className="my-5 flex items-center gap-3 text-[12px] text-ink-3">
              <span className="h-px flex-1 bg-line" />
              hoặc
              <span className="h-px flex-1 bg-line" />
            </div>
            <Button className="w-full" onClick={() => navigate('/hop-dong')}>
              <svg viewBox="0 0 18 18" aria-hidden className="size-4">
                <path fill="#4285F4" d="M17.64 9.2c0-.64-.06-1.25-.16-1.84H9v3.48h4.84a4.14 4.14 0 0 1-1.8 2.72v2.26h2.92a8.78 8.78 0 0 0 2.68-6.62Z" />
                <path fill="#34A853" d="M9 18c2.43 0 4.47-.8 5.96-2.18l-2.92-2.26c-.8.54-1.84.86-3.04.86-2.34 0-4.32-1.58-5.03-3.7H.96v2.33A9 9 0 0 0 9 18Z" />
                <path fill="#FBBC05" d="M3.97 10.72a5.41 5.41 0 0 1 0-3.44V4.95H.96a9 9 0 0 0 0 8.1l3.01-2.33Z" />
                <path fill="#EA4335" d="M9 3.58c1.32 0 2.5.45 3.44 1.35l2.58-2.58A9 9 0 0 0 .96 4.95l3.01 2.33C4.68 5.16 6.66 3.58 9 3.58Z" />
              </svg>
              Tiếp tục với Google
            </Button>
          </>
        )}
      </main>

      <p className="mt-6 max-w-[44ch] text-center text-[12px] text-ink-3">
        ContractGuard hỗ trợ ra quyết định, không thay thế tư vấn pháp lý.{' '}
        {/* shortcut: link chỉ để nhóm xem màn hình lời mời trong demo, xóa khi có email mời thật. */}
        {!invited && (
          <Link to="/dang-nhap?loi-moi=1" className="font-medium text-ink-2 underline-offset-2 hover:underline">
            Xem màn hình nhận lời mời
          </Link>
        )}
      </p>
    </div>
  )
}
