import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeAll, describe, expect, it, vi } from "vitest";

import type { Schemas } from "~/lib/api";

import { AnalysisProgress } from "./AnalysisProgress";

const job = { jobId: "job-1", contractId: "c", status: "running", step: "evaluating", failureReason: null, activity: null } as unknown as Schemas["JobStatus"];

describe("AnalysisProgress", () => {
  // jsdom has no modal dialogs.
  beforeAll(() => {
    HTMLDialogElement.prototype.showModal ??= function (this: HTMLDialogElement) {
      this.open = true;
    };
    HTMLDialogElement.prototype.close ??= function (this: HTMLDialogElement) {
      this.open = false;
    };
  });
  afterEach(() => vi.restoreAllMocks());

  it("stops the run only after the user confirms, then reloads the page data", async () => {
    const fetch = vi.spyOn(globalThis, "fetch").mockImplementation((input) =>
      Promise.resolve(
        String(input).endsWith("/auth/csrf")
          ? new Response(JSON.stringify({ token: "t" }), { status: 200, headers: { "Content-Type": "application/json" } })
          : new Response(null, { status: 204 }),
      ),
    );
    const onFinished = vi.fn();
    render(<AnalysisProgress job={job} onFinished={onFinished} />);

    await userEvent.click(screen.getByRole("button", { name: "Dừng phân tích" }));
    expect(fetch).not.toHaveBeenCalled();

    const dialog = await screen.findByRole("dialog");
    await userEvent.click(within(dialog).getByRole("button", { name: "Dừng phân tích" }));

    await waitFor(() => expect(onFinished).toHaveBeenCalled());
    const cancelCall = fetch.mock.calls.find(([url]) => String(url).endsWith("/jobs/job-1/cancel"));
    expect(cancelCall?.[1]?.method).toBe("POST");
  });
});
