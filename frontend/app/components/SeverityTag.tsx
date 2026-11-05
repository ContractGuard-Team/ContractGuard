export type Severity = "critical" | "high" | "medium" | "low";

const labels: Record<Severity, string> = {
  critical: "Nghiêm trọng",
  high: "Cao",
  medium: "Trung bình",
  low: "Thấp",
};

const textColor: Record<Severity, string> = {
  critical: "text-severity-critical",
  high: "text-severity-high",
  medium: "text-severity-medium",
  low: "text-severity-low",
};

// DESIGN §5 Severity Tag (signature): a 12px shape + the word, in the severity color.
// Shape and word make the level readable without color (The Never-Alone Rule).
export function SeverityTag({ severity }: { severity: Severity }) {
  return (
    <span className={`inline-flex items-center gap-1.5 text-label ${textColor[severity]}`}>
      <SeverityShape severity={severity} />
      {labels[severity]}
    </span>
  );
}

export function SeverityShape({ severity }: { severity: Severity }) {
  const markColor = severity === "medium" ? "text-severity-medium-mark" : "";
  return (
    <svg className={`size-3 shrink-0 ${markColor}`} viewBox="0 0 12 12" aria-hidden="true" data-shape={shapeName[severity]}>
      {severity === "critical" && <path d="M6 0.5 11.5 6 6 11.5 0.5 6Z" fill="currentColor" />}
      {severity === "high" && <path d="M6 1 11.5 11H0.5Z" fill="currentColor" />}
      {severity === "medium" && <circle cx="6" cy="6" r="5" fill="currentColor" />}
      {severity === "low" && <circle cx="6" cy="6" r="4.25" fill="none" stroke="currentColor" strokeWidth="1.5" />}
    </svg>
  );
}

const shapeName: Record<Severity, string> = {
  critical: "diamond",
  high: "triangle",
  medium: "circle",
  low: "ring",
};
