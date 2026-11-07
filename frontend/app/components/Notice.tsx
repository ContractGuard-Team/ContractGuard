import type { ReactNode } from "react";

// A message inside the page. "error" is announced at once by screen readers, "info" politely.
// A tint plus a full border: DESIGN §6 forbids colored side stripes.
export function Notice({ tone, children }: { tone: "error" | "info"; children: ReactNode }) {
  return tone === "error" ? (
    <div role="alert" className="rounded-md border border-severity-critical/30 bg-severity-critical-wash px-3 py-2.5 text-body text-ink">
      {children}
    </div>
  ) : (
    <div role="status" className="rounded-md border border-rule bg-review-ink-wash px-3 py-2.5 text-body text-ink">
      {children}
    </div>
  );
}
