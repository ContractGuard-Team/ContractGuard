import { render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import DocxViewer from "./DocxViewer";

vi.mock("docx-preview", () => ({ renderAsync: vi.fn(async () => {}) }));

describe("DocxViewer", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  // Security: docx-preview does not clean a file. With allow-scripts added, a script inside an uploaded
  // DOCX could read this page and act with the viewer's session.
  it("renders the file in a frame where nothing can run", () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response("x")));

    render(<DocxViewer url="http://storage/a" />);

    expect(screen.getByTitle("Nội dung file DOCX")).toHaveAttribute("sandbox", "allow-same-origin");
  });
});
