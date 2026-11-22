import { useEffect, useState, type ReactNode } from "react";
import { NavLink } from "react-router";

import { useIsWideScreen } from "~/lib/media";

import { Button } from "./Button";
import { Logo } from "./Logo";

interface NavItem {
  to: string;
  label: string;
  icon: ReactNode;
}

const libraryItem: NavItem = { to: "/contracts", label: "Thư viện hợp đồng", icon: <LibraryIcon /> };
const organizationItem: NavItem = { to: "/organization", label: "Thành viên tổ chức", icon: <PeopleIcon /> };
const trashItem: NavItem = { to: "/contracts/trash", label: "Thùng rác", icon: <TrashIcon /> };

export interface WorkspaceOption {
  organizationId: string | null;
  label: string;
}

const railKey = "cg.rail-open";

function readRailOpen() {
  try {
    return window.localStorage.getItem(railKey) !== "false";
  } catch {
    return true;
  }
}

// DESIGN §5 Navigation: 232px rail on the left, primary "upload" button under the logo, 40px nav items,
// a skip link for keyboard users, main content on the desk background. A toggle switches the rail
// between 232px and 56px of icons. On a wide screen the rail pushes the content and remembers its
// state; on a narrow screen, and in a contract workspace (compact) that needs all the width, it stays
// 56px and opens as a drawer over the content.
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
  const navItems = workspace ? [libraryItem, organizationItem, trashItem] : [libraryItem, trashItem];
  const wide = useIsWideScreen();
  const drawer = compact || !wide;
  const [railOpen, setRailOpen] = useState(readRailOpen);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const open = drawer ? drawerOpen : railOpen;
  // Screen readers still get the words the icons replace.
  const labelClass = open ? "" : "sr-only";
  const iconOnlyClass = open ? "" : "justify-center";

  function toggle() {
    if (drawer) {
      setDrawerOpen(!drawerOpen);
      return;
    }

    setRailOpen(!railOpen);
    try {
      window.localStorage.setItem(railKey, String(!railOpen));
    } catch {
      // Private mode: the choice lasts until the tab closes.
    }
  }

  // A drawer closes after the user picks something in it.
  const closeDrawer = () => setDrawerOpen(false);

  useEffect(() => {
    if (!drawerOpen) return;
    const onKey = (event: KeyboardEvent) => event.key === "Escape" && setDrawerOpen(false);
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [drawerOpen]);

  return (
    <div className="flex min-h-screen">
      <a
        href="#main"
        className="sr-only focus:not-sr-only focus:absolute focus:left-2 focus:top-2 focus:z-50 focus:rounded-md focus:bg-sheet focus:px-3 focus:py-2"
      >
        Bỏ qua tới nội dung chính
      </a>

      {/* Keeps the content clear of the rail: as wide as the rail, or as its 56px when it is a drawer. */}
      <div aria-hidden="true" className={`shrink-0 transition-[width] duration-200 ease-out-quart ${open && !drawer ? "w-sidebar" : "w-sidebar-collapsed"}`} />

      {drawerOpen && <div aria-hidden="true" className="fixed inset-0 z-30 bg-ink/30" onClick={closeDrawer} />}

      <aside
        id="app-rail"
        className={`fixed top-0 left-0 z-40 flex h-screen flex-col gap-4 overflow-x-hidden border-r border-rule bg-rail transition-[width] duration-200 ease-out-quart ${open ? "w-sidebar p-3" : "w-sidebar-collapsed p-2"}`}
        aria-label="Điều hướng"
      >
        <div className={`flex h-10 items-center gap-1 ${open ? "justify-between pl-2" : "justify-center"}`}>
          {open && <Logo />}
          <button
            type="button"
            onClick={toggle}
            aria-expanded={open}
            aria-controls="app-rail"
            title={open ? "Thu gọn thanh bên" : "Mở thanh bên"}
            className="inline-flex size-10 shrink-0 items-center justify-center rounded-md text-ink-secondary transition-colors duration-150 hover:bg-ink/5 hover:text-ink"
          >
            <RailIcon open={open} />
            <span className="sr-only">{open ? "Thu gọn thanh bên" : "Mở thanh bên"}</span>
          </button>
        </div>

        {/* FR-ORG-014. Only on the open rail: the select needs the width. */}
        <div className={open ? "" : "hidden"}>
          <label htmlFor="workspace-switcher" className="mb-1 block px-1 text-caption text-ink-secondary">
            Không gian làm việc
          </label>
          <select
            id="workspace-switcher"
            value={workspace ?? ""}
            onChange={(e) => {
              closeDrawer();
              onSwitchWorkspace(e.target.value === "" ? null : e.target.value);
            }}
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
          onClick={() => {
            closeDrawer();
            onUpload();
          }}
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
                  onClick={closeDrawer}
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
          <Button variant="ghost" size="sm" title="Đăng xuất" className={`w-full px-2.5 ${open ? "justify-start" : "justify-center"}`} onClick={onLogout}>
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

// A panel with its left column; the chevron points the way the rail will move.
function RailIcon({ open }: { open: boolean }) {
  return (
    <svg className="size-4 shrink-0" viewBox="0 0 16 16" aria-hidden="true">
      <rect x="2" y="2.5" width="12" height="11" rx="1.5" fill="none" stroke="currentColor" strokeWidth="1.5" />
      <path d="M6 2.5v11" fill="none" stroke="currentColor" strokeWidth="1.5" />
      <path d={open ? "M10.5 6.5 9 8l1.5 1.5" : "M9 6.5 10.5 8 9 9.5"} fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
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

function TrashIcon() {
  return (
    <svg className="size-4 shrink-0" viewBox="0 0 16 16" aria-hidden="true">
      <path d="M2.5 4h11M6 4V2.5h4V4M4 4l.7 9.5h6.6L12 4M6.5 6.5v4.5M9.5 6.5v4.5" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
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
