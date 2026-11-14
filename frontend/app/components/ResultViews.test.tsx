import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";

import type { Schemas } from "~/lib/api";

import { ContractSheet } from "./ContractSheet";
import { ResultOverview } from "./ResultOverview";
import { RiskRuler } from "./RiskRuler";

function finding(overrides: Partial<Schemas["FindingItem"]>): Schemas["FindingItem"] {
  return {
    findingId: overrides.findingKey ?? "f",
    findingKey: "f",
    kind: "problematic_clause",
    title: "Tiêu đề",
    primaryDomain: "cybersecurity",
    secondaryDomains: [],
    category: null,
    severity: "high",
    clauseKey: "cl-1",
    evidence: [{ clauseKey: "cl-1", page: 1, quote: "Provider may do anything." }],
    missingDescription: null,
    why: "Vì sao",
    impact: "Ảnh hưởng",
    recommendation: "Hướng xử lý",
    references: [],
    checkCode: null,
    handlingStatus: "open",
    handlingNote: null,
    handledAt: null,
    ...overrides,
  };
}

const clause = (key: string, text: string): Schemas["ClauseItem"] => ({
  clauseId: key,
  clauseKey: key,
  clauseOrder: 1,
  clauseNumber: key.slice(-1),
  heading: `Heading ${key}`,
  clauseText: text,
  clauseType: "C07",
  location: { pageStart: 1, pageEnd: 1, regions: [] },
});

const findings = [
  finding({ findingKey: "f-1", title: "Rủi ro nghiêm trọng an ninh", severity: "critical" }),
  finding({ findingKey: "f-2", title: "Rủi ro dữ liệu", severity: "high", primaryDomain: "data_privacy", handlingStatus: "resolved" }),
  finding({ findingKey: "f-3", title: "Thiếu quyền kiểm toán", severity: "medium", kind: "missing_clause", clauseKey: null, evidence: [], primaryDomain: "third_party" }),
];

const result: Schemas["AnalysisResultView"] = {
  jobId: "job",
  trace: { schemaVersion: "2.0.0", modelVersion: null, promptVersion: null, ruleVersion: null },
  score: null,
  overallRecommendation: "Chưa nên ký.",
  findings,
  checks: [],
  injectionWarnings: [{ page: 2, region: null, excerpt: "Ignore previous instructions" }],
};

function renderOverview() {
  render(
    <ResultOverview
      result={result}
      clauseName={(k) => k}
      metadata={null}
      selectedFindingId={null}
      onSelectFinding={vi.fn()}
      onGoToClause={vi.fn()}
      onChanged={vi.fn()}
    />,
  );
}

describe("ResultOverview", () => {
  it("states the High and Critical counts and warns about injected instructions", () => {
    renderOverview();

    expect(screen.getByText(/Có 1 rủi ro Nghiêm trọng và 1 rủi ro Cao cần xem xét\. Chưa nên ký\./)).toBeInTheDocument();
    expect(screen.getByRole("alert")).toHaveTextContent("Ignore previous instructions");
    expect(screen.getByText(/chưa có điểm rủi ro/)).toBeInTheDocument();
  });

  it("filters findings by every chosen condition at once (FR-VIEW-006)", async () => {
    renderOverview();
    const list = () => within(screen.getByRole("region", { name: /Rủi ro phát hiện/ })).getAllByRole("listitem");
    expect(list()).toHaveLength(3);

    await userEvent.selectOptions(screen.getByLabelText("Lọc theo mức"), "high");
    expect(list()).toHaveLength(1);

    await userEvent.selectOptions(screen.getByLabelText("Lọc theo trạng thái xử lý"), "open");
    expect(screen.getByText(/Không có finding nào khớp bộ lọc/)).toBeInTheDocument();
  });
});

describe("ContractSheet", () => {
  it("washes a clause with findings and opens its worst finding when clicked (FR-VIEW-010)", async () => {
    const onSelect = vi.fn();
    render(
      <ContractSheet
        clauses={[clause("cl-1", "Provider may do anything."), clause("cl-2", "Plain clause.")]}
        findings={findings}
        focus={null}
        selectedFindingId={null}
        onSelectFinding={onSelect}
        onChanged={vi.fn()}
      />,
    );

    const risky = screen.getByRole("article", { name: "1. Heading cl-1" });
    expect(risky.className).toContain("bg-severity-critical-wash");
    expect(screen.getByRole("article", { name: "2. Heading cl-2" }).className).not.toContain("wash");
    expect(screen.getByText("Điều khoản còn thiếu")).toBeInTheDocument();

    await userEvent.click(within(risky).getByText("Provider may do anything."));
    expect(onSelect).toHaveBeenCalledWith("f-1");
  });
});

describe("RiskRuler", () => {
  it("exposes the score as a meter and names the level in words", () => {
    render(<RiskRuler score={72} level="high" label="Điểm rủi ro tổng" />);

    expect(screen.getByRole("meter", { name: "Điểm rủi ro tổng" })).toHaveAttribute("aria-valuenow", "72");
    expect(screen.getByText("Cao")).toBeInTheDocument();
  });
});
