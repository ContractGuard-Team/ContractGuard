import { fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import { FileViewer } from "./FileViewer";

vi.mock("./PdfViewer", () => ({ default: ({ url }: { url: string }) => <p>pdf: {url}</p> }));
vi.mock("./DocxViewer", () => ({ default: ({ url }: { url: string }) => <p>docx: {url}</p> }));

const link = (url: string) => Response.json({ url });

function setup(fileType: "pdf" | "docx") {
  render(<FileViewer contractId="c1" versionId="v1" fileName="hop-dong" fileType={fileType} />);
}

describe("FileViewer", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it.each([
    ["pdf", "pdf: http://storage/a"],
    ["docx", "docx: http://storage/a"],
  ] as const)("opens a %s file with its viewer", async (fileType, expected) => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(link("http://storage/a")));

    setup(fileType);

    expect(await screen.findByText(expected)).toBeInTheDocument();
  });

  it("says so when the file left storage, and offers no download", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(new Response(JSON.stringify({ code: "ERR-GEN-NOT-FOUND" }), { status: 404, headers: { "Content-Type": "application/problem+json" } })),
    );

    setup("pdf");

    expect(await screen.findByRole("alert")).toHaveTextContent("Không tìm thấy file gốc");
    expect(screen.queryByRole("button", { name: "Tải xuống" })).not.toBeInTheDocument();
  });

  it("asks for a new link when downloading, then says where to find the file", async () => {
    const assign = vi.fn();
    vi.stubGlobal("location", { assign });
    vi.stubGlobal("fetch", vi.fn().mockResolvedValueOnce(link("http://storage/view")).mockResolvedValueOnce(link("http://storage/download")));

    setup("pdf");
    await screen.findByText("pdf: http://storage/view");
    fireEvent.click(screen.getByRole("button", { name: "Tải xuống" }));

    expect(await screen.findByText(/Ctrl\+J/)).toBeInTheDocument();
    expect(assign).toHaveBeenCalledWith("http://storage/download");
  });
});
