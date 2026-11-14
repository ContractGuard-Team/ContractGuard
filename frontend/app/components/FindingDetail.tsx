import { useId, useState, type FormEvent, type ReactNode } from "react";

import { apiPut, type Schemas } from "~/lib/api";
import { errorMessage } from "~/lib/errors";
import { checkNames, handlingStatusLabels, riskDomainLabels } from "~/lib/labels";
import { useAction } from "~/lib/useAction";

import { Button } from "./Button";
import { Notice } from "./Notice";

// FR-VIEW-007: every field of a finding (BR-RISK-014). Quotes are the contract's words, so serif.
export function FindingDetail({
  finding,
  clauseName,
  onGoToClause,
  onHandled,
}: {
  finding: Schemas["FindingItem"];
  clauseName: (clauseKey: string) => string;
  onGoToClause: ((clauseKey: string) => void) | null;
  onHandled: () => void;
}) {
  return (
    <div className="flex flex-col gap-3 text-body">
      <p className="text-caption text-ink-secondary">
        {[riskDomainLabels[finding.primaryDomain], ...finding.secondaryDomains.map((d) => riskDomainLabels[d]), finding.category, finding.checkCode && `${finding.checkCode} · ${checkNames[finding.checkCode] ?? ""}`]
          .filter(Boolean)
          .join(" · ")}
      </p>

      {finding.kind === "missing_clause" ? (
        <p>
          <span className="font-semibold">Thiếu điều khoản.</span> {finding.missingDescription}
        </p>
      ) : (
        finding.evidence.map((evidence, index) => (
          <figure key={index} className="flex flex-col gap-1">
            <blockquote className="rounded-sm bg-desk px-3 py-2 font-contract text-contract">“{evidence.quote}”</blockquote>
            <figcaption className="flex flex-wrap items-center gap-2 text-caption text-ink-muted">
              {clauseName(evidence.clauseKey)} · Trang {evidence.page}
              {onGoToClause && (
                <Button variant="ghost" size="sm" onClick={() => onGoToClause(evidence.clauseKey)}>
                  Xem trong văn bản
                </Button>
              )}
            </figcaption>
          </figure>
        ))
      )}

      <Part title="Vì sao là rủi ro">{finding.why}</Part>
      <Part title="Ảnh hưởng">{finding.impact}</Part>
      <Part title="Hướng xử lý">{finding.recommendation}</Part>
      {finding.references.length > 0 && <Part title="Căn cứ tham khảo">{finding.references.map((r) => `${r.source} ${r.referenceNumber}`).join("; ")}</Part>}

      <HandlingForm finding={finding} onSaved={onHandled} />
    </div>
  );
}

function Part({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div>
      <h4 className="text-label text-ink-secondary">{title}</h4>
      <p className="max-w-prose text-pretty">{children}</p>
    </div>
  );
}

// F-VIEW-003: the team's decision on a finding, shared with everyone who can see the contract.
function HandlingForm({ finding, onSaved }: { finding: Schemas["FindingItem"]; onSaved: () => void }) {
  const id = useId();
  const [status, setStatus] = useState(finding.handlingStatus);
  const [note, setNote] = useState(finding.handlingNote ?? "");
  const save = useAction();
  const changed = status !== finding.handlingStatus || note.trim() !== (finding.handlingNote ?? "");

  function onSubmit(event: FormEvent) {
    event.preventDefault();
    void save.run(async () => {
      await apiPut(`/findings/${finding.findingId}/handling`, { status, note: note.trim() || null });
      onSaved();
    });
  }

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-2 border-t border-rule pt-3">
      <div className="flex flex-wrap items-end gap-2">
        <div className="flex flex-col gap-1">
          <label htmlFor={`${id}-status`} className="text-label">
            Trạng thái xử lý
          </label>
          <select
            id={`${id}-status`}
            value={status}
            onChange={(e) => setStatus(e.target.value as Schemas["HandlingStatus"])}
            className="h-9 rounded-md border border-rule-strong bg-sheet px-2 text-body focus:border-review-ink focus:ring-3 focus:ring-review-ink/15 focus:outline-none"
          >
            {Object.entries(handlingStatusLabels).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
        </div>
        <div className="flex min-w-48 flex-1 flex-col gap-1">
          <label htmlFor={`${id}-note`} className="text-label">
            Ghi chú
          </label>
          <input
            id={`${id}-note`}
            value={note}
            maxLength={2000}
            onChange={(e) => setNote(e.target.value)}
            placeholder="Ví dụ: đã gửi đề nghị sửa cho đối tác"
            className="h-9 rounded-md border border-rule-strong bg-sheet px-3 text-body placeholder:text-ink-muted focus:border-review-ink focus:ring-3 focus:ring-review-ink/15 focus:outline-none"
          />
        </div>
        <Button type="submit" size="sm" variant="secondary" disabled={!changed} loading={save.pending}>
          Lưu
        </Button>
      </div>
      {save.error !== null && <Notice tone="error">{errorMessage(save.error)}</Notice>}
    </form>
  );
}
