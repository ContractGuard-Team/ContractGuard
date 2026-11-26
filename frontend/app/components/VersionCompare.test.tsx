import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";

import type { Schemas } from "~/lib/api";

import { VersionCompare } from "./VersionCompare";

const version = (n: number, status: Schemas["RunStatus"] | null, hasResult = status === "completed") =>
  ({ contractVersionId: `v${n}`, versionNumber: n, originalFileName: `hd-v${n}.pdf`, uploadedAt: "2026-11-20T00:00:00Z", latestRunStatus: status, overallScore: null, riskLevel: null, hasResult }) as Schemas["VersionItem"];

const result = (overall: number, status: Schemas["CheckStatus"]) => ({
  jobId: "j",
  score: { overallScore: overall, riskLevel: "high", domains: [] },
  findings: [],
  checks: [{ checkCode: "CY-03", status, clauseKeys: [], findingKey: null, note: null }],
  injectionWarnings: [],
});

function jsonResponse(body: unknown) {
  return new Response(JSON.stringify(body), { status: 200, headers: { "Content-Type": "application/json" } });
}

describe("VersionCompare (F-CMP-001)", () => {
  afterEach(() => vi.restoreAllMocks());

  it("is hidden while fewer than two versions are analysed", () => {
    const { container } = render(<VersionCompare contractId="c" versions={[version(2, "completed"), version(1, "failed")]} />);

    expect(container).toBeEmptyDOMElement();
  });

  it("compares the two newest analysed versions", async () => {
    vi.spyOn(globalThis, "fetch").mockImplementation((input) => {
      const url = String(input);
      if (url.includes("version=v1")) return Promise.resolve(jsonResponse({ completedJobId: "j1" }));
      if (url.includes("version=v2")) return Promise.resolve(jsonResponse({ completedJobId: "j2" }));
      return Promise.resolve(jsonResponse(url.includes("/jobs/j1/") ? result(40, "pass") : result(62, "missing")));
    });
    // V2 was re-run and failed: its earlier result still counts (FR-ANA-015).
    render(<VersionCompare contractId="c" versions={[version(2, "failed", true), version(1, "completed")]} />);

    await userEvent.click(screen.getByRole("button", { name: "So sánh" }));

    expect(await screen.findByText("+22")).toBeInTheDocument();
    expect(screen.getByText("PASS → MISSING")).toBeInTheDocument();
  });
});
