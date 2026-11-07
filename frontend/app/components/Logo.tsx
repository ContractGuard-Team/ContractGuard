export function Logo({ textClassName = "" }: { textClassName?: string }) {
  return (
    <span className="flex items-center gap-2 text-title text-ink">
      <svg className="size-5 shrink-0 text-review-ink" viewBox="0 0 20 20" aria-hidden="true">
        <path d="M5 2h7l4 4v12H5z" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round" />
        <path d="M8 10h6M8 13h6M8 7h3" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
      </svg>
      <span className={textClassName}>ContractGuard</span>
    </span>
  );
}
