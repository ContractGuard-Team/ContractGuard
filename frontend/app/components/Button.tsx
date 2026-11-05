import type { ButtonHTMLAttributes } from "react";

type Variant = "primary" | "secondary" | "ghost" | "danger";

const variants: Record<Variant, string> = {
  primary: "bg-review-ink text-sheet hover:bg-review-ink-hover active:bg-review-ink-pressed px-4",
  secondary: "bg-sheet text-ink border border-rule-strong hover:bg-desk px-4",
  ghost: "text-ink-secondary hover:bg-ink/6 hover:text-ink px-3",
  danger: "text-severity-critical hover:bg-severity-critical-wash px-3",
};

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant;
  size?: "md" | "sm";
  loading?: boolean;
}

// DESIGN §5 Buttons: 40px high (36px small), 6px radius, 14px semibold, no shadow.
// While loading the button keeps its size and shows a small spinner next to the label.
export function Button({ variant = "primary", size = "md", loading = false, disabled, className = "", children, ...rest }: ButtonProps) {
  const sizing = size === "sm" ? "h-9 text-label font-semibold" : "h-10 text-button";
  return (
    <button
      type="button"
      {...rest}
      disabled={disabled || loading}
      aria-busy={loading || undefined}
      className={`inline-flex items-center justify-center gap-2 rounded-md transition-colors duration-150 ease-out-quart disabled:cursor-not-allowed disabled:opacity-50 ${sizing} ${variants[variant]} ${className}`}
    >
      {loading && <Spinner />}
      {children}
    </button>
  );
}

function Spinner() {
  return (
    <svg className="size-4 animate-spin" viewBox="0 0 16 16" aria-hidden="true">
      <circle cx="8" cy="8" r="6" fill="none" stroke="currentColor" strokeOpacity="0.3" strokeWidth="2" />
      <path d="M14 8a6 6 0 0 0-6-6" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
    </svg>
  );
}
