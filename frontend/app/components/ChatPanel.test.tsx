import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";

import { parseSse } from "~/lib/api";

import { ChatPanel, suggestedQuestions } from "./ChatPanel";

function jsonResponse(body: unknown) {
  return new Response(JSON.stringify(body), { status: 200, headers: { "Content-Type": "application/json" } });
}

function sseResponse(text: string) {
  return new Response(text, { status: 200, headers: { "Content-Type": "text/event-stream" } });
}

const stored = (messages: unknown[]) => ({ available: true, messages });

describe("ChatPanel", () => {
  afterEach(() => vi.restoreAllMocks());

  it("stays locked and says why while no assistant is connected", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse({ available: false, messages: [] }));
    render(<ChatPanel contractId="c" hasResult clauseName={(k) => k} onCitation={vi.fn()} />);

    expect(await screen.findByText(/Trợ lý AI chưa được kết nối/)).toBeInTheDocument();
    expect(screen.getByLabelText("Câu hỏi về hợp đồng")).toBeDisabled();
    expect(screen.getByRole("button", { name: "Gửi" })).toBeDisabled();
  });

  it("streams an answer and jumps to the cited clause (FR-CHAT-004)", async () => {
    const answer = {
      chatMessageId: "a",
      role: "assistant",
      authorName: null,
      content: "Điều 6 loại trừ trách nhiệm.",
      citations: [{ clauseKey: "cl-006", quote: null }],
      versionNumber: 1,
      createdAt: "2026-11-10T04:00:00Z",
    };
    const fetchMock = vi
      .spyOn(globalThis, "fetch")
      .mockResolvedValueOnce(jsonResponse(stored([])))
      .mockResolvedValueOnce(jsonResponse({ token: "t" }))
      .mockResolvedValueOnce(
        sseResponse('event: question\ndata: {"messageId":"q"}\n\nevent: token\ndata: {"text":"Điều 6 "}\n\nevent: citation\ndata: {"citation":{"clauseKey":"cl-006","quote":null}}\n\nevent: done\ndata: {"messageId":"a"}\n\n'),
      )
      .mockResolvedValueOnce(jsonResponse(stored([{ ...answer, chatMessageId: "q", role: "user", authorName: "An", content: "Ai chịu?", citations: [] }, answer])));
    const onCitation = vi.fn();
    render(<ChatPanel contractId="c" hasResult clauseName={(k) => `Điều ${k}`} onCitation={onCitation} />);

    await userEvent.type(await screen.findByLabelText("Câu hỏi về hợp đồng"), "Ai chịu?");
    await userEvent.click(screen.getByRole("button", { name: "Gửi" }));

    await waitFor(() => expect(screen.getByText("Điều 6 loại trừ trách nhiệm.")).toBeInTheDocument());
    expect(fetchMock.mock.calls[2]![0]).toBe("/api/v1/contracts/c/chat");
    await userEvent.click(screen.getByRole("button", { name: "Điều cl-006" }));
    expect(onCitation).toHaveBeenCalledWith("cl-006");
  });

  it("sends a suggested question as a question (FR-CHAT-008, FR-CHAT-009)", async () => {
    // By request rather than by order: the antiforgery token may already be cached by an earlier test.
    const fetchMock = vi.spyOn(globalThis, "fetch").mockImplementation((input, init) => {
      const url = String(input);
      if (url.endsWith("/chat") && init?.method === "POST") {
        return Promise.resolve(sseResponse('event: token\ndata: {"text":"Có."}\n\nevent: done\ndata: {"messageId":"a"}\n\n'));
      }

      return Promise.resolve(jsonResponse(url.endsWith("/chat") ? stored([]) : { token: "t" }));
    });
    render(<ChatPanel contractId="c" hasResult clauseName={(k) => k} onCitation={vi.fn()} suggestions={["Rủi ro lớn nhất là gì?"]} />);

    await userEvent.click(await screen.findByRole("button", { name: "Rủi ro lớn nhất là gì?" }));

    await waitFor(() => expect(fetchMock.mock.calls.some(([, init]) => init?.method === "POST")).toBe(true));
    const post = fetchMock.mock.calls.find(([, init]) => init?.method === "POST")!;
    expect(JSON.parse(post[1]!.body as string)).toEqual({ question: "Rủi ro lớn nhất là gì?" });
  });
});

describe("parseSse", () => {
  it("returns complete events and keeps an unfinished tail", () => {
    const { events, rest } = parseSse('event: token\ndata: {"text":"a"}\n\nevent: done\r\ndata: {}\r\n\r\nevent: tok');

    expect(events).toEqual([
      { event: "token", data: '{"text":"a"}' },
      { event: "done", data: "{}" },
    ]);
    expect(rest).toBe("event: tok");
  });
});

describe("suggestedQuestions", () => {
  it("asks first about the clauses with the worst findings, each once", () => {
    const questions = suggestedQuestions(
      [
        { clauseKey: "cl-1", severity: "low" },
        { clauseKey: "cl-2", severity: "critical" },
        { clauseKey: "cl-2", severity: "high" },
        { clauseKey: null, severity: "critical" },
        { clauseKey: "cl-3", severity: "high" },
      ],
      (k) => `Điều ${k}`,
    );

    expect(questions).toHaveLength(4);
    expect(questions[0]).toContain("Điều cl-2");
    expect(questions[1]).toContain("Điều cl-3");
  });

  it("still offers general questions without findings", () => {
    expect(suggestedQuestions([], (k) => k).length).toBeGreaterThan(0);
  });
});
