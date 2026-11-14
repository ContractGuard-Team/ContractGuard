import { Fragment, useEffect } from "react";

import type { Schemas } from "~/lib/api";
import { clauseTypeLabels, severityOrder } from "~/lib/labels";

import { FindingDetail } from "./FindingDetail";
import { severityLabels, SeverityShape, SeverityTag } from "./SeverityTag";

export const severityWash: Record<Schemas["Severity"], string> = {
  critical: "bg-severity-critical-wash",
  high: "bg-severity-high-wash",
  medium: "bg-severity-medium-wash",
  low: "bg-severity-low-wash",
};

export const clauseElementId = (clauseKey: string) => `clause-${clauseKey}`;

// F-VIEW-002 on the clause text (DESIGN §5 Contract Sheet and Margin Note): the clauses on one white sheet,
// each clause with findings washed in its worst severity, the findings as margin notes beside it.
// Clicking a clause or a note opens the finding right under the clause (FR-VIEW-010); "go to clause"
// from elsewhere scrolls here and marks the clause (FR-VIEW-011).
// Narrower than sheet + margin (948px): the notes move under the clause heading (P3-02, docs/04).
export function ContractSheet({
  clauses,
  findings,
  focus,
  selectedFindingId,
  onSelectFinding,
  onChanged,
}: {
  clauses: Schemas["ClauseItem"][];
  findings: Schemas["FindingItem"][];
  // A new object each time, so going to the same clause twice scrolls twice.
  focus: { clauseKey: string } | null;
  selectedFindingId: string | null;
  onSelectFinding: (findingId: string | null) => void;
  onChanged: () => void;
}) {
  const byClause = new Map<string, Schemas["FindingItem"][]>();
  for (const finding of findings) {
    if (finding.clauseKey) {
      byClause.set(finding.clauseKey, [...(byClause.get(finding.clauseKey) ?? []), finding]);
    }
  }

  const missing = findings.filter((f) => f.kind === "missing_clause");
  const clauseName = (key: string) => {
    const clause = clauses.find((c) => c.clauseKey === key);
    return clause ? [clause.clauseNumber, clause.heading].filter(Boolean).join(". ") || key : key;
  };

  const focusedClauseKey = focus?.clauseKey ?? null;
  useEffect(() => {
    if (focus) {
      document.getElementById(clauseElementId(focus.clauseKey))?.scrollIntoView({ behavior: "smooth", block: "center" });
    }
  }, [focus]);

  return (
    <section aria-labelledby="clauses-title" className="@container py-4">
      <h2 id="clauses-title" className="mx-auto mb-3 max-w-sheet text-title @min-[948px]:max-w-[948px]">
        {clauses.length} điều khoản đã phân loại
      </h2>

      {missing.length > 0 && (
        <div className="mx-auto mb-4 flex max-w-sheet flex-col gap-1 rounded-md border border-rule bg-sheet px-4 py-3 @min-[948px]:max-w-[948px]">
          <p className="text-label text-ink-secondary">Điều khoản còn thiếu</p>
          {missing.map((finding) => (
            <NoteButton key={finding.findingId} finding={finding} selected={finding.findingId === selectedFindingId} onSelect={onSelectFinding} />
          ))}
          {missing.some((f) => f.findingId === selectedFindingId) && (
            <div className="pt-2">
              <FindingDetail finding={missing.find((f) => f.findingId === selectedFindingId)!} clauseName={clauseName} onGoToClause={null} onHandled={onChanged} />
            </div>
          )}
        </div>
      )}

      <div className="relative mx-auto grid max-w-sheet grid-cols-1 @min-[948px]:max-w-[948px] @min-[948px]:grid-cols-[var(--spacing-sheet)_var(--spacing-margin-note)] @min-[948px]:gap-x-6">
        {/* One sheet behind the clause column, so washed clauses do not break the paper into cards. */}
        <div aria-hidden="true" className="absolute inset-y-0 left-0 w-full rounded-sheet bg-sheet shadow-sheet @min-[948px]:w-sheet" />

        {clauses.map((clause, index) => {
          const notes = sortBySeverity(byClause.get(clause.clauseKey) ?? []);
          const worst = notes[0]?.severity;
          const focused = clause.clauseKey === focusedClauseKey;
          const selected = notes.find((n) => n.findingId === selectedFindingId);
          const tint = focused ? "bg-review-ink-wash" : worst ? severityWash[worst] : "";

          return (
            <Fragment key={clause.clauseId}>
              <article
                id={clauseElementId(clause.clauseKey)}
                aria-label={clauseName(clause.clauseKey)}
                className={`relative scroll-mt-16 px-8 py-6 transition-colors duration-200 ${index > 0 ? "border-t border-rule" : ""} ${tint} ${worst ? "cursor-pointer" : ""}`}
                onClick={(event) => {
                  // Only the clause itself, not the form of an open finding inside it.
                  if (worst && !(event.target as HTMLElement).closest("[data-finding-detail]")) {
                    onSelectFinding(selected ? null : notes[0]!.findingId);
                  }
                }}
              >
                <p className="mb-2 flex flex-wrap items-baseline justify-between gap-x-4 text-label">
                  <span className="text-ink">{clauseTypeLabels[clause.clauseType] ?? clause.clauseType}</span>
                  <span className="flex items-center gap-3 font-normal text-ink-muted">
                    {worst && <SeverityTag severity={worst} />}
                    {pageLabel(clause.location)}
                  </span>
                </p>

                {/* Narrow layout: the margin notes sit here instead of beside the sheet. */}
                {notes.length > 0 && (
                  <div className="mb-2 flex flex-col gap-1 @min-[948px]:hidden">
                    {notes.map((finding) => (
                      <NoteButton key={finding.findingId} finding={finding} selected={finding.findingId === selectedFindingId} onSelect={onSelectFinding} />
                    ))}
                  </div>
                )}

                {(clause.clauseNumber || clause.heading) && (
                  <h3 className="mb-1 font-contract text-contract font-semibold">{[clause.clauseNumber, clause.heading].filter(Boolean).join(". ")}</h3>
                )}
                <p className="font-contract text-contract whitespace-pre-line">{clause.clauseText}</p>

                {selected && (
                  <div data-finding-detail className="mt-4 cursor-auto rounded-md border border-rule bg-sheet p-4">
                    <p className="mb-2 font-semibold">{selected.title}</p>
                    <FindingDetail finding={selected} clauseName={clauseName} onGoToClause={null} onHandled={onChanged} />
                  </div>
                )}
              </article>

              <aside aria-label={`Ghi chú cho ${clauseName(clause.clauseKey)}`} className="hidden flex-col gap-1 py-6 @min-[948px]:flex">
                {notes.map((finding) => (
                  <NoteButton key={finding.findingId} finding={finding} selected={finding.findingId === selectedFindingId} onSelect={onSelectFinding} margin />
                ))}
              </aside>
            </Fragment>
          );
        })}
      </div>
    </section>
  );
}

// DESIGN §5 Margin Note: severity shape + one-line summary, tied to its clause by a dashed rule.
// Also used by the sample sheet of the landing page, hence only the three fields it shows.
export function NoteButton({
  finding,
  selected,
  onSelect,
  margin = false,
}: {
  finding: Pick<Schemas["FindingItem"], "findingId" | "severity" | "title">;
  selected: boolean;
  onSelect: (findingId: string | null) => void;
  margin?: boolean;
}) {
  return (
    <button
      type="button"
      aria-expanded={selected}
      onClick={(event) => {
        event.stopPropagation();
        onSelect(selected ? null : finding.findingId);
      }}
      className={`flex items-start gap-2 rounded-md px-2 py-1.5 text-left text-label transition-colors duration-150 hover:bg-ink/5 ${margin ? "border-l border-dashed border-rule-strong" : ""} ${selected ? "bg-review-ink-wash" : ""}`}
    >
      <span className="mt-0.5">
        <SeverityShape severity={finding.severity} />
      </span>
      <span>
        <span className="sr-only">{severityLabels[finding.severity]}: </span>
        {finding.title}
      </span>
    </button>
  );
}

function sortBySeverity(findings: Schemas["FindingItem"][]) {
  return [...findings].sort((a, b) => severityOrder.indexOf(a.severity) - severityOrder.indexOf(b.severity));
}

function pageLabel({ pageStart, pageEnd }: Schemas["ClauseLocation"]) {
  return pageStart === pageEnd ? `Trang ${pageStart}` : `Trang ${pageStart}–${pageEnd}`;
}
