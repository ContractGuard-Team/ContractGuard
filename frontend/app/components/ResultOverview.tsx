import { useState, type ReactNode } from "react";

import type { Schemas } from "~/lib/api";
import { handlingStatusLabels, riskDomainLabels, severityOrder } from "~/lib/labels";

import { FindingDetail } from "./FindingDetail";
import { Notice } from "./Notice";
import { RiskRuler } from "./RiskRuler";
import { severityLabels, SeverityShape, SeverityTag } from "./SeverityTag";

// F-VIEW-001: scores, counts, overall recommendation and the findings with filters and details.
export function ResultOverview({
  result,
  clauseName,
  metadata,
  selectedFindingId,
  onSelectFinding,
  onGoToClause,
  onChanged,
}: {
  result: Schemas["AnalysisResultView"];
  clauseName: (clauseKey: string) => string;
  metadata: ReactNode;
  selectedFindingId: string | null;
  onSelectFinding: (findingId: string | null) => void;
  onGoToClause: (clauseKey: string) => void;
  onChanged: () => void;
}) {
  const counts = Object.fromEntries(severityOrder.map((s) => [s, result.findings.filter((f) => f.severity === s).length])) as Record<Schemas["Severity"], number>;

  return (
    <div className="mx-auto flex w-full max-w-sheet flex-col gap-8 py-4">
      {result.injectionWarnings.length > 0 && (
        <Notice tone="error">
          <p className="font-semibold">File có đoạn văn giống câu lệnh gửi cho AI</p>
          <p>Kết quả có thể bị ảnh hưởng. Hãy đọc kỹ các đoạn này trong văn bản gốc:</p>
          <ul className="mt-1 list-disc pl-5">
            {result.injectionWarnings.map((warning, index) => (
              <li key={index}>
                Trang {warning.page}: “{warning.excerpt}”
              </li>
            ))}
          </ul>
        </Notice>
      )}

      <section aria-labelledby="score-title" className="flex flex-col gap-4">
        <h2 id="score-title" className="sr-only">
          Điểm rủi ro
        </h2>
        {result.score?.riskLevel ? (
          <>
            <RiskRuler score={Number(result.score.overallScore)} level={result.score.riskLevel} label="Điểm rủi ro tổng" />
            <table className="w-full text-left text-body">
              <caption className="mb-2 text-left text-label text-ink-secondary">Điểm theo nhóm rủi ro</caption>
              <tbody className="divide-y divide-rule">
                {result.score.domains.map((domain) => (
                  <tr key={domain.domain}>
                    <th scope="row" className="py-2 pr-4 font-medium whitespace-nowrap">
                      {riskDomainLabels[domain.domain]}
                    </th>
                    {domain.applicable && domain.riskLevel ? (
                      <td className="py-2">
                        <span className="mr-3 tabular-nums">{domain.score}/100</span>
                        <SeverityTag severity={domain.riskLevel} />
                        {domain.reEnabled && <span className="ml-3 text-caption text-ink-muted">Bật lại vì hợp đồng có điều khoản thuộc nhóm này; nên kiểm tra lại ngữ cảnh.</span>}
                      </td>
                    ) : (
                      <td className="py-2 text-ink-secondary">
                        <span className="mr-2 font-medium">N/A</span>
                        {domain.naReason}
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </>
        ) : (
          <Notice tone="info">Lần phân tích này chưa có điểm rủi ro. Phần chấm điểm của dịch vụ AI chưa gửi kết quả; finding và checklist bên dưới vẫn đầy đủ.</Notice>
        )}
      </section>

      <section aria-labelledby="recommendation-title" className="flex flex-col gap-2">
        <h2 id="recommendation-title" className="text-title">
          Khuyến nghị tổng
        </h2>
        <ul className="flex flex-wrap gap-x-5 gap-y-1" aria-label="Số finding theo mức">
          {severityOrder.map((severity) => (
            <li key={severity} className="flex items-center gap-1.5">
              <SeverityTag severity={severity} />
              <span className="tabular-nums">{counts[severity]}</span>
            </li>
          ))}
        </ul>
        <p className="max-w-prose">
          Có {counts.critical} rủi ro Nghiêm trọng và {counts.high} rủi ro Cao cần xem xét.
          {result.overallRecommendation && ` ${result.overallRecommendation}`}
        </p>
      </section>

      {metadata}

      <FindingList
        findings={result.findings}
        clauseName={clauseName}
        selectedFindingId={selectedFindingId}
        onSelectFinding={onSelectFinding}
        onGoToClause={onGoToClause}
        onChanged={onChanged}
      />
    </div>
  );
}

// FR-VIEW-006: filter by domain, severity and handling status, all conditions together.
function FindingList({
  findings,
  clauseName,
  selectedFindingId,
  onSelectFinding,
  onGoToClause,
  onChanged,
}: {
  findings: Schemas["FindingItem"][];
  clauseName: (clauseKey: string) => string;
  selectedFindingId: string | null;
  onSelectFinding: (findingId: string | null) => void;
  onGoToClause: (clauseKey: string) => void;
  onChanged: () => void;
}) {
  const [domain, setDomain] = useState("");
  const [severity, setSeverity] = useState("");
  const [handling, setHandling] = useState("");
  const shown = findings.filter((f) => (!domain || f.primaryDomain === domain) && (!severity || f.severity === severity) && (!handling || f.handlingStatus === handling));
  const filterClass = "h-9 rounded-md border border-rule-strong bg-sheet px-2 text-body focus:border-review-ink focus:ring-3 focus:ring-review-ink/15 focus:outline-none";

  return (
    <section aria-labelledby="findings-title" className="flex flex-col gap-3">
      <h2 id="findings-title" className="text-title">
        Rủi ro phát hiện ({findings.length})
      </h2>
      <div className="flex flex-wrap gap-2" role="group" aria-label="Lọc finding">
        <select aria-label="Lọc theo nhóm" value={domain} onChange={(e) => setDomain(e.target.value)} className={filterClass}>
          <option value="">Mọi nhóm</option>
          {Object.entries(riskDomainLabels).map(([value, label]) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </select>
        <select aria-label="Lọc theo mức" value={severity} onChange={(e) => setSeverity(e.target.value)} className={filterClass}>
          <option value="">Mọi mức</option>
          {severityOrder.map((value) => (
            <option key={value} value={value}>
              {severityLabels[value]}
            </option>
          ))}
        </select>
        <select aria-label="Lọc theo trạng thái xử lý" value={handling} onChange={(e) => setHandling(e.target.value)} className={filterClass}>
          <option value="">Mọi trạng thái</option>
          {Object.entries(handlingStatusLabels).map(([value, label]) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </select>
      </div>

      {shown.length === 0 ? (
        <p className="text-ink-secondary">{findings.length === 0 ? "Không phát hiện rủi ro nào trong hợp đồng này." : "Không có finding nào khớp bộ lọc. Hãy bỏ bớt điều kiện lọc."}</p>
      ) : (
        <ul className="divide-y divide-rule rounded-md border border-rule bg-sheet">
          {shown.map((finding) => {
            const open = finding.findingId === selectedFindingId;
            return (
              <li key={finding.findingId} id={`finding-${finding.findingId}`}>
                <button
                  type="button"
                  aria-expanded={open}
                  onClick={() => onSelectFinding(open ? null : finding.findingId)}
                  className="flex w-full items-start gap-3 px-4 py-3 text-left transition-colors duration-150 hover:bg-desk"
                >
                  <span className="mt-1">
                    <SeverityShape severity={finding.severity} />
                  </span>
                  <span className="flex min-w-0 flex-1 flex-col">
                    <span className="font-medium">{finding.title}</span>
                    <span className="text-caption text-ink-secondary">
                      <SeverityTag severity={finding.severity} /> · {riskDomainLabels[finding.primaryDomain]} ·{" "}
                      {finding.clauseKey ? clauseName(finding.clauseKey) : "Thiếu điều khoản"} · {handlingStatusLabels[finding.handlingStatus]}
                    </span>
                  </span>
                </button>
                {open && (
                  <div className="px-4 pb-4 pl-10">
                    <FindingDetail finding={finding} clauseName={clauseName} onGoToClause={onGoToClause} onHandled={onChanged} />
                  </div>
                )}
              </li>
            );
          })}
        </ul>
      )}
    </section>
  );
}
