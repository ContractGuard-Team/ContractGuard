import type { Schemas } from "~/lib/api";
import { checkNames, checkStatusLabels } from "~/lib/labels";

import { Button } from "./Button";

// Status as a word plus a weight, never color alone. MISSING and PARTIAL read stronger than PASS and N/A.
const statusClass: Record<Schemas["CheckStatus"], string> = {
  pass: "text-severity-low",
  partial: "font-semibold text-severity-high",
  missing: "font-semibold text-severity-critical",
  not_applicable: "text-ink-muted",
};

// FR-VIEW-012: the 20 checks of BR-RISK-007, each with its status and a link to its clauses.
export function Checklist({
  checks,
  clauseName,
  onGoToClause,
}: {
  checks: Schemas["CheckResult"][];
  clauseName: (clauseKey: string) => string;
  onGoToClause: (clauseKey: string) => void;
}) {
  const failing = checks.filter((c) => c.status === "partial" || c.status === "missing").length;

  return (
    <section aria-labelledby="checklist-title" className="mx-auto flex w-full max-w-sheet flex-col gap-3 py-4">
      <h2 id="checklist-title" className="text-title">
        Checklist 20 check
      </h2>
      <p className="text-ink-secondary">
        {failing} check PARTIAL hoặc MISSING. Mỗi check này sinh một rủi ro trong tab Tổng quan.
      </p>
      <div className="overflow-x-auto rounded-md border border-rule bg-sheet">
        <table className="w-full text-left text-body">
          <thead className="border-b border-rule text-label text-ink-secondary">
            <tr>
              <th scope="col" className="px-3 py-2 font-medium whitespace-nowrap">
                Mã
              </th>
              <th scope="col" className="px-3 py-2 font-medium whitespace-nowrap">
                Check
              </th>
              <th scope="col" className="px-3 py-2 font-medium whitespace-nowrap">
                Trạng thái
              </th>
              <th scope="col" className="px-3 py-2 font-medium whitespace-nowrap">
                Điều khoản
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-rule">
            {checks.map((check) => (
              <tr key={check.checkCode}>
                <td className="px-3 py-2 whitespace-nowrap tabular-nums">{check.checkCode}</td>
                <td className="px-3 py-2">
                  {checkNames[check.checkCode] ?? check.checkCode}
                  {check.note && <span className="block text-caption text-ink-muted">{check.note}</span>}
                </td>
                <td className={`px-3 py-2 whitespace-nowrap ${statusClass[check.status]}`}>{checkStatusLabels[check.status]}</td>
                <td className="px-3 py-1">
                  {check.clauseKeys.length === 0 ? (
                    <span className="text-ink-muted">—</span>
                  ) : (
                    check.clauseKeys.map((key) => (
                      <Button key={key} variant="ghost" size="sm" className="!px-1.5 text-left" onClick={() => onGoToClause(key)}>
                        {clauseName(key)}
                      </Button>
                    ))
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
