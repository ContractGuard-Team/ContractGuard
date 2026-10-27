import { FileText, LogOut, Plus, Users } from 'lucide-react'
import type { ReactNode } from 'react'
import { Link, NavLink } from 'react-router'
import { ORG_SHORT } from '../demo/mock'
import { useStore } from '../demo/store'
import { Avatar, Toasts, buttonClass, cx } from './ui'

export function LogoMark({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 32 32" aria-hidden className={cx('size-7 shrink-0', className)}>
      <path d="M7 3h13l6 6v20H7z" className="fill-primary" />
      <path d="M20 3v6h6" fill="oklch(0.74 0.06 200)" />
      <path d="M11 15h11M11 19h11M11 23h7" stroke="white" strokeWidth="2" strokeLinecap="round" />
    </svg>
  )
}

export function Wordmark({ className }: { className?: string }) {
  return (
    <span className={cx('inline-flex items-center gap-2', className)}>
      <LogoMark />
      <span className="text-[15px] font-semibold tracking-[-0.01em] text-ink">ContractGuard</span>
    </span>
  )
}

const NAV = [
  { to: '/hop-dong', label: 'Hợp đồng', icon: FileText },
  { to: '/to-chuc', label: 'Tổ chức', icon: Users },
]

/** Sidebar đầy đủ cho trang danh sách; dạng thu gọn (rail) cho workspace để nhường chỗ cho văn bản. */
export function AppShell({ children, compact = false }: { children: ReactNode; compact?: boolean }) {
  const { me, toasts } = useStore()
  return (
    <div className="min-h-dvh md:flex">
      <a
        href="#main"
        className="sr-only focus:not-sr-only focus:fixed focus:top-2 focus:left-2 focus:rounded-md focus:bg-paper focus:px-3 focus:py-2 focus:shadow"
        style={{ zIndex: 'var(--z-toast)' }}
      >
        Bỏ qua tới nội dung chính
      </a>

      <aside
        className={cx(
          'sticky top-0 hidden h-dvh shrink-0 flex-col border-r border-line bg-sidebar md:flex',
          compact ? 'w-14 items-center py-3' : 'w-[232px] px-3 py-4',
        )}
      >
        <Link to="/hop-dong" aria-label="ContractGuard, về thư viện hợp đồng" className={cx('rounded-md', compact ? 'p-1' : 'px-2 py-1')}>
          {compact ? <LogoMark /> : <Wordmark />}
        </Link>
        {!compact && <p className="mt-1 px-2 text-[12.5px] text-ink-3">{ORG_SHORT}</p>}

        {!compact && (
          <Link to="/hop-dong/moi" className={cx(buttonClass('primary'), 'mt-5 w-full')}>
            <Plus aria-hidden className="size-4" />
            Tải hợp đồng lên
          </Link>
        )}

        <nav aria-label="Điều hướng chính" className={cx('flex flex-col gap-0.5', compact ? 'mt-5' : 'mt-4')}>
          {compact && (
            <Link to="/hop-dong/moi" title="Tải hợp đồng lên" aria-label="Tải hợp đồng lên" className={cx(buttonClass('primary'), 'mb-2 size-9 px-0')}>
              <Plus aria-hidden className="size-4" />
            </Link>
          )}
          {NAV.map(({ to, label, icon: Icon }) => (
            <NavLink
              key={to}
              to={to}
              title={compact ? label : undefined}
              aria-label={compact ? label : undefined}
              className={({ isActive }) =>
                cx(
                  'flex items-center gap-2.5 rounded-md text-[13.5px] font-medium transition-colors duration-150',
                  compact ? 'size-9 justify-center' : 'h-9 px-2.5',
                  isActive ? 'bg-paper text-ink shadow-[0_0_0_1px_var(--color-line)]' : 'text-ink-2 hover:bg-ink/[0.05] hover:text-ink',
                )
              }
            >
              <Icon aria-hidden className="size-4 shrink-0" />
              {!compact && label}
            </NavLink>
          ))}
        </nav>

        <div className={cx('mt-auto flex items-center gap-2.5', compact ? 'flex-col' : 'border-t border-line px-1 pt-3')}>
          <Avatar name={me.name} size={30} />
          {!compact && (
            <div className="min-w-0 flex-1">
              <p className="truncate text-[13px] font-medium">{me.name}</p>
              <p className="truncate text-[12px] text-ink-3">{me.email}</p>
            </div>
          )}
          <Link to="/dang-nhap" aria-label="Đăng xuất" title="Đăng xuất" className={cx(buttonClass('ghost', 'sm'), 'size-8 px-0')}>
            <LogOut aria-hidden className="size-4" />
          </Link>
        </div>
      </aside>

      {/* Thanh trên cho màn hình hẹp */}
      <header className="sticky top-0 flex h-12 items-center gap-2 border-b border-line bg-sidebar px-3 md:hidden" style={{ zIndex: 'var(--z-sticky)' }}>
        <Link to="/hop-dong" aria-label="ContractGuard, về thư viện hợp đồng" className="mr-auto rounded-md">
          <Wordmark />
        </Link>
        {NAV.map(({ to, label, icon: Icon }) => (
          <NavLink
            key={to}
            to={to}
            aria-label={label}
            className={({ isActive }) => cx(buttonClass('ghost', 'sm'), 'size-9 px-0', isActive && 'bg-paper text-ink')}
          >
            <Icon aria-hidden className="size-4" />
          </NavLink>
        ))}
      </header>

      <main id="main" tabIndex={-1} className="min-w-0 flex-1 focus:outline-none">
        {children}
      </main>
      <Toasts toasts={toasts} />
    </div>
  )
}

export function EmptyState({ title, children, action }: { title: string; children?: ReactNode; action?: ReactNode }) {
  return (
    <div className="mx-auto flex max-w-md flex-col items-center px-6 py-16 text-center">
      <svg viewBox="0 0 64 64" aria-hidden className="mb-4 size-14 text-line-strong">
        <path d="M16 6h22l10 10v42H16z" fill="var(--color-paper)" stroke="currentColor" strokeWidth="2" />
        <path d="M38 6v10h10" fill="none" stroke="currentColor" strokeWidth="2" />
        <path d="M23 30h18M23 37h18M23 44h11" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeDasharray="2 4" />
      </svg>
      <h2 className="text-[15px] font-semibold">{title}</h2>
      {children && <div className="mt-1.5 text-[13.5px] text-ink-2">{children}</div>}
      {action && <div className="mt-5">{action}</div>}
    </div>
  )
}
