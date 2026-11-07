import type { ReactNode } from "react";

import { Logo } from "./Logo";

// Shared frame of the pages before login: the logo, one form on a sheet, links underneath.
export function AuthPage({ title, children, footer }: { title: string; children: ReactNode; footer?: ReactNode }) {
  return (
    <main className="flex min-h-screen flex-col items-center px-4 pt-[12vh] pb-8">
      <div className="w-full max-w-sm">
        <div className="mb-6">
          <Logo />
        </div>
        <div className="rounded-md border border-rule bg-sheet p-6">
          <h1 className="mb-4 text-headline">{title}</h1>
          {children}
        </div>
        {footer && <p className="mt-4 text-ink-secondary">{footer}</p>}
      </div>
    </main>
  );
}

export const linkClass = "text-review-ink underline underline-offset-2 hover:text-review-ink-hover";
