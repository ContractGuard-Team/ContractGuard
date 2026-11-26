import { render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import DocxViewer, { matchParagraphs } from "./DocxViewer";

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

describe("matchParagraphs (FR-VIEW-009 on DOCX)", () => {
  const clauses = [
    { clauseKey: "cl-1", text: "Điều 1. Phạm vi\nBên B cung cấp dịch vụ lưu trữ đám mây cho Bên A." },
    { clauseKey: "cl-2", text: "Điều 2. Bảo mật\nBên B thông báo sự cố trong vòng 72 giờ.\nBên B mã hóa dữ liệu khi lưu trữ." },
  ];

  it("finds the clause of each paragraph, ignoring spacing and case", () => {
    const keys = matchParagraphs(["Điều 1. Phạm vi", "Bên B  cung cấp dịch vụ lưu trữ đám mây cho Bên A.", "BÊN B thông báo sự cố trong vòng 72 giờ."], clauses);

    expect(keys).toEqual(["cl-1", "cl-1", "cl-2"]);
  });

  it("leaves short and unknown paragraphs alone", () => {
    expect(matchParagraphs(["1.", "Phụ lục không thuộc điều khoản nào"], clauses)).toEqual([null, null]);
  });

  it("prefers the clause after the last match when a sentence repeats", () => {
    const repeated = [
      { clauseKey: "cl-1", text: "Điều 1. Các bên thống nhất như sau." },
      { clauseKey: "cl-2", text: "Điều 2. Bảo mật. Các bên thống nhất như sau." },
    ];

    expect(matchParagraphs(["Điều 2. Bảo mật.", "Các bên thống nhất như sau."], repeated)).toEqual(["cl-2", "cl-2"]);
  });
});
