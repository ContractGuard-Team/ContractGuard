import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { afterEach, describe, expect, it, vi } from "vitest";

import type { Schemas } from "~/lib/api";

import { VersionsPanel } from "./VersionsPanel";

const version = (n: number, deletedAt: string | null = null) =>
  ({ contractVersionId: `v${n}`, versionNumber: n, originalFileName: `hd-v${n}.pdf`, uploadedAt: "2026-11-20T00:00:00Z", latestRunStatus: null, overallScore: null, riskLevel: null, hasResult: false, deletedAt }) as Schemas["VersionItem"];

function renderPanel(versions: Schemas["VersionItem"][], onChanged = vi.fn().mockResolvedValue(undefined)) {
  render(
    <MemoryRouter>
      <VersionsPanel contractId="c" versions={versions} shownVersionId="v1" busy={false} isOwner onUploaded={vi.fn()} onChanged={onChanged} />
    </MemoryRouter>,
  );
  return onChanged;
}

describe("VersionsPanel", () => {
  afterEach(() => vi.restoreAllMocks());

  it("lists a deleted version with a restore button that calls the API", async () => {
    const fetch = vi
      .spyOn(globalThis, "fetch")
      .mockImplementation(async (input) => (String(input).endsWith("/auth/csrf") ? Response.json({ token: "t" }) : new Response(null, { status: 204 })));
    const onChanged = renderPanel([version(2, "2026-11-28T00:00:00Z"), version(1)]);

    expect(screen.getByText(/Đã xóa lúc/)).toBeInTheDocument();
    // V1 is the only active version: it cannot be deleted.
    expect(screen.queryByRole("button", { name: "Xóa" })).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Khôi phục" }));

    expect(fetch.mock.calls.some(([url]) => String(url).includes("/contracts/c/versions/v2/restore"))).toBe(true);
    expect(onChanged).toHaveBeenCalled();
  });

  it("offers delete while more than one version is active", () => {
    renderPanel([version(2), version(1)]);

    expect(screen.getAllByRole("button", { name: "Xóa" })).toHaveLength(2);
  });
});
