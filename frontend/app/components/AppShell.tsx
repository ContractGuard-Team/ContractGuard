import type { ReactNode } from "react";
import { NavLink } from "react-router";

import { Button } from "./Button";
import { Logo } from "./Logo";

interface NavItem {
  to: string;
  label: string;
  icon: ReactNode;
}

const libraryItem: NavItem = { to: "/", label: "Thư viện hợp đồng", icon: <LibraryIcon /> };
const organizationItem: NavItem = { to: "/organization", label: "Thành viên tổ chức", icon: <PeopleIcon /> };

export interface WorkspaceOption {
  organizationId: string | null;
  label: string;
}

// DESIGN §5 Navigation: 232px rail on the left, primary "upload" button under the logo, 40px nav items,
// a skip link for keyboard users, main content on the desk background. The rail shrinks to 56px of icons
// on a narrow screen, and always in a contract workspace (compact), which then gets all the height.
export function AppShell({
  children,
  compact,
  name,
  email,
  workspaces,
  workspace,
  onSwitchWorkspace,
  onUpload,
  onLogout,
}: {
  children: ReactNode;
  compact: boolean;
  name: string;
  email: string;
  workspaces: WorkspaceOption[];
  workspace: string | null;
  onSwitchWorkspace: (organizationId: string | null | "new") => void;
  onUpload: () => void;
  onLogout: () => void;
}) {
  const navItems = workspace ? [libraryItem, organizationItem] : [libraryItem];
  // Screen readers still get the words the icons replace.
  const labelClass = compact ? "sr-only" : "max-lg:sr-only";
  const iconOnlyClass = compact ? "justify-center" : "max-lg:justify-center";

  return (
    <div className="flex min-h-screen">
      <a
        href="#main"
        className="sr-only focus:not-sr-only focus:absolute focus:left-2 focus:top-2 focus:z-50 focus:rounded-md focus:bg-sheet focus:px-3 focus:py-2"
      >
        Bỏ qua tới nội dung chính
      </a>

      <aside
        className={`sticky top-0 flex h-screen shrink-0 flex-col gap-4 border-r border-rule bg-rail p-2 ${compact ? "w-sidebar-collapsed" : "w-sidebar-collapsed lg:w-sidebar lg:p-3"}`}
        aria-label="Điều hướng"
      >
        <div className={`flex h-10 items-center px-2 ${iconOnlyClass}`}>
          <Logo textClassName={labelClass} />
        </div>

        {/* FR-ORG-014. Hidden on the narrow rail: switching from inside a contract makes no sense. */}
        <div className={compact ? "hidden" : "max-lg:hidden"}>
          <label htmlFor="workspace-switcher" className="mb-1 block px-1 text-caption text-ink-secondary">
            Không gian làm việc
          </label>
          <select
            id="workspace-switcher"
            value={workspace ?? ""}
            onChange={(e) => onSwitchWorkspace(e.target.value === "" ? null : e.target.value)}
            className="h-10 w-full rounded-md border border-rule-strong bg-sheet px-2 text-label font-semibold text-ink focus:border-review-ink focus:ring-3 focus:ring-review-ink/15 focus:outline-none"
          >
            {workspaces.map((option) => (
              <option key={option.organizationId ?? "personal"} value={option.organizationId ?? ""}>
                {option.label}
              </option>
            ))}
            <option value="new">+ Tạo tổ chức mới</option>
          </select>
        </div>

        <button
          type="button"
          onClick={onUpload}
          title="Tải hợp đồng lên"
          className="inline-flex h-10 w-full items-center justify-center gap-2 rounded-md bg-review-ink text-button text-sheet transition-colors duration-150 ease-out-quart hover:bg-review-ink-hover active:bg-review-ink-pressed"
        >
          <UploadIcon />
          <span className={labelClass}>Tải hợp đồng lên</span>
        </button>

        <nav>
          <ul className="flex flex-col gap-1">
            {navItems.map((item) => (
              <li key={item.to}>
                <NavLink
                  to={item.to}
                  end
                  title={item.label}
                  className={({ isActive }) =>
                    `flex h-10 items-center gap-2.5 rounded-md border px-2.5 text-button transition-colors duration-150 ${iconOnlyClass} ${
                      isActive ? "border-rule bg-sheet text-ink" : "border-transparent text-ink-secondary hover:bg-ink/5"
                    }`
                  }
                >
                  {item.icon}
                  <span className={labelClass}>{item.label}</span>
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>

        <div className="mt-auto border-t border-rule pt-3">
          <div className={labelClass}>
            <p className="truncate px-2.5 text-label font-medium text-ink" title={name}>
              {name}
            </p>
            <p className="truncate px-2.5 pb-1 text-caption text-ink-secondary" title={email}>
              {email}
            </p>
          </div>
          <Button variant="ghost" size="sm" title="Đăng xuất" className={`w-full px-2.5 lg:justify-start ${iconOnlyClass}`} onClick={onLogout}>
            <LogoutIcon />
            <span className={labelClass}>Đăng xuất</span>
          </Button>
        </div>
      </aside>

      <main id="main" className={compact ? "h-screen min-w-0 flex-1 overflow-hidden" : "min-w-0 flex-1 p-4 lg:p-8"}>
        {children}
      </main>
    </div>
  );
}

function UploadIcon() {
  return (
    <svg className="size-4 shrink-0" viewBox="0 0 16 16" aria-hidden="true">
      <path d="M8 11V2.5M4.5 6 8 2.5 11.5 6M2.5 13.5h11" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

function LibraryIcon() {
  return (
    <svg className="size-4 shrink-0" viewBox="0 0 16 16" aria-hidden="true">
      <path d="M3 2.5h7l3 3v8H3z" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round" />
    </svg>
  );
}

function PeopleIcon() {
  return (
    <svg className="size-4 shrink-0" viewBox="0 0 16 16" aria-hidden="true">
      <circle cx="6" cy="5.5" r="2.5" fill="none" stroke="currentColor" strokeWidth="1.5" />
      <path d="M1.5 13.5c.5-2.5 2.3-4 4.5-4s4 1.5 4.5 4M10.5 3.5a2.5 2.5 0 0 1 0 5M12.5 9.8c1 .6 1.8 1.8 2 3.7" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
    </svg>
  );
}

function LogoutIcon() {
  return (
    <svg className="size-4 shrink-0" viewBox="0 0 16 16" aria-hidden="true">
      <path d="M6.5 2.5h-4v11h4M10.5 5l3 3-3 3M13.5 8h-7" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}
