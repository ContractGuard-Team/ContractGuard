import { type FormEvent, useState } from "react";

import { apiFetch, type Schemas } from "~/lib/api";
import { type Change, type Comparison, compareResults } from "~/lib/compare";
import { errorMessage } from "~/lib/errors";
import { riskDomainLabels } from "~/lib/labels";
import { useAction } from "~/lib/useAction";

import { Button } from "./Button";
import { SelectField } from "./Field";
import { Notice } from "./Notice";
import { SeverityTag } from "./SeverityTag";

// The stored result of a version: its detail names the run to show (FR-CTR-016), then that run's result.
async function loadResult(contractId: string, versionId: string) {
  const detail = await apiFetch<Schemas["ContractDetail"]>(`/contracts/${contractId}?version=${versionId}`);
  if (!detail.completedJobId) {
    throw new Error("Phiên bản này chưa có kết quả phân tích.");
  }

  return apiFetch<Schemas["AnalysisResultView"]>(`/jobs/${detail.completedJobId}/result`);
}

function formatDelta(before: number | null, after: number | null) {
  if (before === null || after === null) {
    return "–";
  }

  const delta = after - before;
  return delta > 0 ? `+${delta}` : String(delta);
}

function ChangeList({ title, changes, empty }: { title: string; changes: Change[]; empty: string }) {
  return (
    <div className="flex flex-col gap-2">
      <h4 className="text-label">{title}</h4>
      {changes.length === 0 ? (
        <p className="text-ink-secondary">{empty}</p>
      ) : (
        <ul className="divide-y divide-rule rounded-md border border-rule bg-sheet">
          {changes.map((change) => (
            <li key={change.key} className="flex flex-wrap items-center justify-between gap-2 px-3 py-2">
              <span className="min-w-0">
                <span className="block">{change.title}</span>
                <span className="text-caption text-ink-secondary">{change.detail}</span>
              </span>
              {change.severity && <SeverityTag severity={change.severity} />}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

// F-CMP-001 (FR-CTR-020): pick two analysed versions and see the clauses and checks that got weaker and
// how the risk score moved. Offered only when at least two versions have a completed analysis.
export function VersionCompare({ contractId, versions }: { contractId: string; versions: Schemas["VersionItem"][] }) {
  // Versions come newest first; by default the newest analysed one is compared with the one before it.
  const analysed = versions.filter((v) => v.hasResult);
  // Empty until the user picks, so a version analysed after the page opened still becomes the default.
  const [pickedFrom, setFromId] = useState("");
  const [pickedTo, setToId] = useState("");
  const fromId = pickedFrom || (analysed[1]?.contractVersionId ?? "");
  const toId = pickedTo || (analysed[0]?.contractVersionId ?? "");
  const [comparison, setComparison] = useState<Comparison | null>(null);
  const load = useAction();

  if (analysed.length < 2) {
    return null;
  }

  const name = (id: string) => `V${versions.find((v) => v.contractVersionId === id)?.versionNumber ?? "?"}`;

  function compare(event: FormEvent) {
    event.preventDefault();
    setComparison(null);
    void load.run(async () => {
      const [before, after] = await Promise.all([loadResult(contractId, fromId), loadResult(contractId, toId)]);
      setComparison(compareResults(before, after));
    });
  }

  return (
    <section aria-labelledby="compare-title" className="flex flex-col gap-3">
      <h3 id="compare-title" className="text-label">
        So sánh hai phiên bản
      </h3>
      <form className="flex flex-wrap items-end gap-3" onSubmit={compare}>
        <SelectField label="Phiên bản trước" value={fromId} onChange={(e) => setFromId(e.target.value)}>
          {analysed.map((v) => (
            <option key={v.contractVersionId} value={v.contractVersionId}>
              V{v.versionNumber} · {v.originalFileName}
            </option>
          ))}
        </SelectField>
        <SelectField label="Phiên bản sau" value={toId} onChange={(e) => setToId(e.target.value)}>
          {analysed.map((v) => (
            <option key={v.contractVersionId} value={v.contractVersionId}>
              V{v.versionNumber} · {v.originalFileName}
            </option>
          ))}
        </SelectField>
        <Button type="submit" variant="secondary" size="sm" disabled={fromId === toId} loading={load.pending}>
          So sánh
        </Button>
      </form>
      {fromId === toId && <p className="text-caption text-ink-secondary">Chọn hai phiên bản khác nhau.</p>}
      {load.error !== null && <Notice tone="error">{errorMessage(load.error)}</Notice>}

      {comparison && (
        <div className="flex flex-col gap-4">
          <table className="w-full border-collapse text-left">
            <caption className="sr-only">
              Điểm rủi ro {name(fromId)} và {name(toId)}
            </caption>
            <thead className="text-caption text-ink-secondary">
              <tr>
                <th className="py-1 font-normal">Nhóm</th>
                <th className="py-1 text-right font-normal">{name(fromId)}</th>
                <th className="py-1 text-right font-normal">{name(toId)}</th>
                <th className="py-1 text-right font-normal">Thay đổi</th>
              </tr>
            </thead>
            <tbody className="tabular-nums">
              {comparison.scores.map((s) => (
                <tr key={s.domain ?? "overall"} className={s.domain === null ? "font-semibold" : ""}>
                  <td className="border-t border-rule py-1">{s.domain === null ? "Điểm tổng" : riskDomainLabels[s.domain]}</td>
                  <td className="border-t border-rule py-1 text-right">{s.before ?? "N/A"}</td>
                  <td className="border-t border-rule py-1 text-right">{s.after ?? "N/A"}</td>
                  <td className="border-t border-rule py-1 text-right">{formatDelta(s.before, s.after)}</td>
                </tr>
              ))}
            </tbody>
          </table>
          <ChangeList title={`Bị yếu đi ở ${name(toId)}`} changes={comparison.weakened} empty="Không có điều khoản hay check nào bị yếu đi." />
          <ChangeList title={`Được cải thiện ở ${name(toId)}`} changes={comparison.improved} empty="Không có thay đổi theo hướng tốt hơn." />
        </div>
      )}
    </section>
  );
}
