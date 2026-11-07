import type { Schemas } from "~/lib/api";
import { clauseTypeLabels } from "~/lib/labels";

// F-ANA-002: the clauses on one contract sheet (DESIGN §5). The contract's own words use the serif
// voice; what the system adds (type, page) uses the sans voice.
export function ClauseList({ clauses }: { clauses: Schemas["ClauseItem"][] }) {
  return (
    <section aria-labelledby="clauses-title" className="mx-auto w-full max-w-sheet">
      <h2 id="clauses-title" className="mb-3 text-title">
        {clauses.length} điều khoản đã phân loại
      </h2>
      <ol className="divide-y divide-rule rounded-sheet bg-sheet px-8 shadow-sheet">
        {clauses.map((clause) => (
          <li key={clause.clauseId} className="py-6">
            <p className="mb-2 flex flex-wrap items-baseline justify-between gap-x-4 text-label">
              <span className="text-ink">{clauseTypeLabels[clause.clauseType] ?? clause.clauseType}</span>
              <span className="font-normal text-ink-muted">{pageLabel(clause.location)}</span>
            </p>
            {(clause.clauseNumber || clause.heading) && (
              <h3 className="mb-1 font-contract text-contract font-semibold">{[clause.clauseNumber, clause.heading].filter(Boolean).join(". ")}</h3>
            )}
            <p className="font-contract text-contract whitespace-pre-line">{clause.clauseText}</p>
          </li>
        ))}
      </ol>
    </section>
  );
}

function pageLabel({ pageStart, pageEnd }: Schemas["ClauseLocation"]) {
  return pageStart === pageEnd ? `Trang ${pageStart}` : `Trang ${pageStart}–${pageEnd}`;
}
