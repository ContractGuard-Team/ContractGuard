import type { Schemas } from "~/lib/api";

import { SeverityTag } from "./SeverityTag";

// BR-RISK-010 thresholds: [from, to) per level.
const zones: { level: NonNullable<Schemas["RiskLevel"]>; from: number; to: number; solid: string; wash: string }[] = [
  { level: "low", from: 0, to: 30, solid: "bg-severity-low", wash: "bg-severity-low-wash" },
  { level: "medium", from: 30, to: 55, solid: "bg-severity-medium-mark", wash: "bg-severity-medium-wash" },
  { level: "high", from: 55, to: 85, solid: "bg-severity-high", wash: "bg-severity-high-wash" },
  { level: "critical", from: 85, to: 101, solid: "bg-severity-critical", wash: "bg-severity-critical-wash" },
];

// DESIGN §5 "Thước rủi ro": an 8px bar split at the thresholds, the current zone solid, the others washed,
// with a tick at the score. Replaces any donut gauge. The number and level word carry the meaning.
export function RiskRuler({ score, level, label }: { score: number; level: NonNullable<Schemas["RiskLevel"]>; label: string }) {
  return (
    <div className="flex flex-col gap-1.5">
      <p className="flex flex-wrap items-baseline gap-x-3 gap-y-1">
        <span className="text-label text-ink-secondary">{label}</span>
        <span className="text-title tabular-nums">{score}/100</span>
        <SeverityTag severity={level} />
      </p>
      <div
        role="meter"
        aria-label={label}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={score}
        aria-valuetext={`${score} trên 100`}
        className="relative flex h-2 w-full max-w-md gap-px"
      >
        {zones.map((zone) => (
          <span key={zone.level} className={`h-full first:rounded-l-full last:rounded-r-full ${zone.level === level ? zone.solid : zone.wash}`} style={{ flexGrow: zone.to - zone.from }} />
        ))}
        <span aria-hidden="true" className="absolute -top-1 h-4 w-0.5 rounded bg-ink" style={{ left: `calc(${Math.min(score, 100)}% - 1px)` }} />
      </div>
    </div>
  );
}
