import { describe, expect, it } from "vitest";

import type { Schemas } from "./api";
import { compareResults } from "./compare";

function finding(overrides: Partial<Schemas["FindingItem"]>): Schemas["FindingItem"] {
  return {
    findingId: "f",
    findingKey: "fk",
    kind: "clause_risk",
    title: "Thông báo sự cố chậm",
    primaryDomain: "cybersecurity",
    secondaryDomains: [],
    category: "incident_notice",
    severity: "medium",
    clauseKey: "cl-1",
    evidence: [],
    missingDescription: null,
    why: "",
    impact: "",
    recommendation: "",
    references: [],
    checkCode: null,
    handlingStatus: "open",
    handlingNote: null,
    handledAt: null,
    ...overrides,
  } as Schemas["FindingItem"];
}

const check = (checkCode: string, status: Schemas["CheckStatus"]) => ({ checkCode, status, clauseKeys: [], findingKey: null, note: null });
const score = (overall: number, cyber: number) => ({
  overallScore: overall,
  riskLevel: "high" as const,
  domains: [{ domain: "cybersecurity" as const, applicable: true, score: cyber, riskLevel: "high" as const, naReason: null, reEnabled: false }],
});

describe("compareResults (FR-CTR-020)", () => {
  it("flags the incident-notice clause as weakened and shows the score delta", () => {
    const before = { score: score(40, 50), findings: [finding({ severity: "medium" })], checks: [check("CY-03", "pass")] };
    const after = { score: score(62, 80), findings: [finding({ severity: "high", findingKey: "other-key" })], checks: [check("CY-03", "partial")] };

    const result = compareResults(before, after);

    expect(result.scores[0]).toEqual({ domain: null, before: 40, after: 62 });
    expect(result.scores[1]).toEqual({ domain: "cybersecurity", before: 50, after: 80 });
    expect(result.weakened.map((c) => c.detail)).toEqual(["Trung bình → Cao", "PASS → PARTIAL"]);
    expect(result.improved).toEqual([]);
  });

  it("lists new findings as weakened and vanished ones as improved, ignoring N/A checks", () => {
    const before = { score: null, findings: [finding({ category: "liability", title: "Giới hạn trách nhiệm thấp" })], checks: [check("PR-01", "not_applicable")] };
    const after = { score: null, findings: [finding({})], checks: [check("PR-01", "missing")] };

    const result = compareResults(before, after);

    expect(result.weakened).toHaveLength(1);
    expect(result.weakened[0]!.detail).toBe("Phát sinh ở phiên bản sau");
    expect(result.improved[0]!.title).toBe("Giới hạn trách nhiệm thấp");
    expect(result.scores[0]).toEqual({ domain: null, before: null, after: null });
  });

  it("keeps findings that share a category apart", () => {
    const two = [finding({ findingId: "a", clauseKey: "cl-1" }), finding({ findingId: "b", clauseKey: "cl-2", severity: "high" })];

    const removedOne = compareResults({ score: null, findings: two, checks: [] }, { score: null, findings: [two[1]!], checks: [] });
    const addedOne = compareResults({ score: null, findings: [two[0]!], checks: [] }, { score: null, findings: two, checks: [] });

    expect(removedOne.improved.map((c) => c.detail)).toEqual(["2 → 1 finding"]);
    expect(addedOne.weakened.map((c) => c.detail)).toEqual(["Trung bình → Cao"]);
  });
});
