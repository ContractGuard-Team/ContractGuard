import type { ReactNode } from "react";
import { NavLink } from "react-router";

interface NavItem {
  to: string;
  label: string;
  icon: ReactNode;
}

const navItems: NavItem[] = [{ to: "/", label: "Thư viện hợp đồng", icon: <LibraryIcon /> }];

// DESIGN §5 Navigation: 232px rail on the left, primary "upload" button under the logo,
// 40px nav items, a skip link for keyboard users, main content on the desk background.
export function AppShell({ children, onUpload }: { children: ReactNode; onUpload?: () => void }) {
  return (
    <div className="flex min-h-screen">
      <a
        href="#main"
        className="sr-only focus:not-sr-only focus:absolute focus:left-2 focus:top-2 focus:z-50 focus:rounded-md focus:bg-sheet focus:px-3 focus:py-2"
      >
        Bỏ qua tới nội dung chính
      </a>

      <aside className="flex w-sidebar shrink-0 flex-col gap-4 border-r border-rule bg-rail p-3" aria-label="Điều hướng">
        <div className="flex h-10 items-center gap-2 px-2 text-title text-ink">
          <Logo />
          ContractGuard
        </div>

        <button
          type="button"
          onClick={onUpload}
          className="inline-flex h-10 w-full items-center justify-center gap-2 rounded-md bg-review-ink text-button text-sheet transition-colors duration-150 ease-out-quart hover:bg-review-ink-hover active:bg-review-ink-pressed"
        >
          <UploadIcon />
          Tải hợp đồng lên
        </button>

        <nav>
          <ul className="flex flex-col gap-1">
            {navItems.map((item) => (
              <li key={item.to}>
                <NavLink
                  to={item.to}
                  end
                  className={({ isActive }) =>
                    `flex h-10 items-center gap-2.5 rounded-md border px-2.5 text-button transition-colors duration-150 ${
                      isActive ? "border-rule bg-sheet text-ink" : "border-transparent text-ink-secondary hover:bg-ink/5"
                    }`
                  }
                >
                  {item.icon}
                  {item.label}
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>
      </aside>

      <main id="main" className="min-w-0 flex-1 p-8">
        {children}
      </main>
    </div>
  );
}

function Logo() {
  return (
    <svg className="size-5 text-review-ink" viewBox="0 0 20 20" aria-hidden="true">
      <path d="M5 2h7l4 4v12H5z" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round" />
      <path d="M8 10h6M8 13h6M8 7h3" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
    </svg>
  );
}

function UploadIcon() {
  return (
    <svg className="size-4" viewBox="0 0 16 16" aria-hidden="true">
      <path d="M8 11V2.5M4.5 6 8 2.5 11.5 6M2.5 13.5h11" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

function LibraryIcon() {
  return (
    <svg className="size-4" viewBox="0 0 16 16" aria-hidden="true">
      <path d="M3 2.5h7l3 3v8H3z" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round" />
    </svg>
  );
}
