import { useCallback, useEffect, useId, useRef, useState, type FormEvent, type KeyboardEvent } from "react";

import { apiFetch, apiStream, type Schemas } from "~/lib/api";
import { errorMessage } from "~/lib/errors";
import { severityOrder } from "~/lib/labels";

import { Button } from "./Button";

// A stored message, or one still streaming (no time or version yet).
type Message = Omit<Schemas["ChatMessageItem"], "createdAt" | "versionNumber"> & Partial<Pick<Schemas["ChatMessageItem"], "createdAt" | "versionNumber">>;

// Data of one SSE event of POST /contracts/{id}/chat (ChatEvent in the API).
interface ChatEventData {
  text?: string | null;
  citation?: Schemas["ChatCitation"] | null;
  code?: string | null;
}

// FR-CHAT-008: questions offered when the chat opens. Built here from the result already on the page, so they
// need no model: the two clauses with the worst findings, then general questions. A later worker field
// (docs/06 §3.4) could replace them.
export function suggestedQuestions(findings: Pick<Schemas["FindingItem"], "clauseKey" | "severity">[], clauseName: (clauseKey: string) => string): string[] {
  const worstClauses = [...findings]
    .filter((f) => f.clauseKey !== null)
    .sort((a, b) => severityOrder.indexOf(a.severity) - severityOrder.indexOf(b.severity))
    .map((f) => f.clauseKey!)
    .filter((key, index, keys) => keys.indexOf(key) === index)
    .slice(0, 2);
  return [
    ...worstClauses.map((key) => `${clauseName(key)} có rủi ro gì và nên đề nghị sửa thế nào?`),
    "Rủi ro lớn nhất của hợp đồng này là gì?",
    "Ai chịu trách nhiệm khi dữ liệu bị lộ?",
  ].slice(0, 4);
}

const time = new Intl.DateTimeFormat("vi-VN", { dateStyle: "short", timeStyle: "short" });

// UI-GEN-002, F-CHAT-001, F-CHAT-003: the contract's shared conversation beside the document. Answers
// stream in (SD-15); each citation jumps to its clause (FR-CHAT-004). Sans-serif throughout: this is the
// system's voice (DESIGN §3). While no model is connected it stays locked and says so, with no made-up answers.
export function ChatPanel({
  contractId,
  hasResult,
  clauseName,
  onCitation,
  suggestions = [],
}: {
  contractId: string;
  hasResult: boolean;
  suggestions?: string[];
  clauseName: (clauseKey: string) => string;
  onCitation: (clauseKey: string) => void;
}) {
  const inputId = useId();
  const [thread, setThread] = useState<Schemas["ChatThread"] | null>(null);
  const [loadError, setLoadError] = useState<unknown>(null);
  const [question, setQuestion] = useState("");
  const [pending, setPending] = useState<Message[]>([]);
  const [sending, setSending] = useState(false);
  const [sendError, setSendError] = useState<unknown>(null);
  const log = useRef<HTMLDivElement>(null);

  const fetchThread = useCallback(() => apiFetch<Schemas["ChatThread"]>(`/contracts/${contractId}/chat`), [contractId]);

  useEffect(() => {
    let current = true;
    fetchThread()
      .then((loaded) => current && setThread(loaded))
      .catch((error: unknown) => current && setLoadError(error));
    return () => {
      current = false;
    };
  }, [fetchThread]);

  const messages = [...(thread?.messages ?? []), ...pending];
  useEffect(() => {
    if (log.current) {
      log.current.scrollTop = log.current.scrollHeight;
    }
  }, [messages.length, pending]);

  const canAsk = thread?.available === true && hasResult;

  // FR-CHAT-009: a suggestion is sent as it stands, like a typed question.
  async function ask(event?: FormEvent, suggestion?: string) {
    event?.preventDefault();
    const text = (suggestion ?? question).trim();
    if (!text || sending) {
      return;
    }

    setSending(true);
    setSendError(null);
    if (suggestion === undefined) {
      setQuestion("");
    }
    const answer: Message = { chatMessageId: "pending-answer", role: "assistant", authorName: null, content: "", citations: [] };
    setPending([{ chatMessageId: "pending-question", role: "user", authorName: "Bạn", content: text, citations: [] }, answer]);
    let failed = false;
    try {
      await apiStream(`/contracts/${contractId}/chat`, { question: text }, ({ event, data }) => {
        const payload = JSON.parse(data) as ChatEventData;
        if (event === "token") {
          answer.content += payload.text ?? "";
        } else if (event === "citation" && payload.citation) {
          answer.citations = [...answer.citations, payload.citation];
        } else if (event === "error") {
          failed = true;
        }

        setPending((current) => [current[0]!, { ...answer }]);
      });
    } catch (error) {
      setSendError(error);
      setQuestion(text);
    } finally {
      if (failed) {
        setSendError(new Error("ERR-CHAT-001"));
      }

      try {
        setThread(await fetchThread());
      } catch (error) {
        setLoadError(error);
      }

      setPending([]);
      setSending(false);
    }
  }

  function onKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === "Enter" && !event.shiftKey) {
      event.preventDefault();
      void ask();
    }
  }

  return (
    <div className="flex min-h-0 flex-1 flex-col">
      <div ref={log} role="log" aria-label="Hội thoại với trợ lý AI" aria-busy={sending} className="flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto p-4">
        {thread === null && loadError === null && <div className="h-16 animate-pulse rounded-md bg-rule/40" aria-hidden="true" />}
        {loadError !== null && <p className="text-severity-critical">{errorMessage(loadError)}</p>}
        {thread !== null && !thread.available && (
          <p className="text-ink-secondary">Trợ lý AI chưa được kết nối. Khi sẵn sàng, bạn có thể hỏi về hợp đồng đang mở tại đây và nhận câu trả lời kèm trích dẫn điều khoản.</p>
        )}
        {thread?.available && !hasResult && <p className="text-ink-secondary">Hãy chờ phân tích xong. Trợ lý trả lời dựa trên các điều khoản đã tách của phiên bản đang mở.</p>}
        {thread?.available && hasResult && messages.length === 0 && (
          <p className="text-ink-secondary">Hỏi về hợp đồng đang mở, ví dụ “Ai chịu trách nhiệm khi dữ liệu bị lộ?”. Mọi người có quyền xem hợp đồng đều thấy cuộc hội thoại này.</p>
        )}

        {messages.map((message) => (
          <article key={message.chatMessageId} aria-label={message.role === "user" ? `Câu hỏi của ${message.authorName ?? "người dùng"}` : "Câu trả lời của trợ lý"} className={message.role === "user" ? "self-end max-w-[85%]" : "max-w-full"}>
            <p className="mb-1 text-caption text-ink-muted">
              {message.role === "user" ? message.authorName : "Trợ lý AI"}
              {message.createdAt && ` · ${time.format(new Date(message.createdAt))}`}
              {message.versionNumber !== undefined && ` · V${message.versionNumber}`}
            </p>
            <div className={message.role === "user" ? "rounded-md bg-review-ink-wash px-3 py-2" : "whitespace-pre-line"}>
              {message.content || (message.role === "assistant" && <span className="text-ink-muted">Đang trả lời…</span>)}
            </div>
            {message.citations.length > 0 && (
              <ul className="mt-2 flex flex-wrap gap-1.5" aria-label="Trích dẫn">
                {message.citations.map((citation) => (
                  <li key={citation.clauseKey}>
                    <button
                      type="button"
                      onClick={() => onCitation(citation.clauseKey)}
                      className="rounded-md border border-rule-strong px-2 py-1 text-label text-ink transition-colors duration-150 hover:bg-desk"
                    >
                      {clauseName(citation.clauseKey)}
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </article>
        ))}
      </div>
      <form className="flex flex-col gap-2 border-t border-rule p-3" onSubmit={(event) => void ask(event)}>
        {canAsk && !sending && suggestions.length > 0 && (
          <ul className="flex flex-wrap gap-1.5" aria-label="Câu hỏi gợi ý">
            {suggestions.map((suggestion) => (
              <li key={suggestion}>
                <button
                  type="button"
                  onClick={() => void ask(undefined, suggestion)}
                  className="rounded-md border border-rule-strong px-2 py-1 text-left text-label text-ink transition-colors duration-150 hover:bg-desk"
                >
                  {suggestion}
                </button>
              </li>
            ))}
          </ul>
        )}
        <label htmlFor={inputId} className="sr-only">
          Câu hỏi về hợp đồng
        </label>
        {sendError !== null && (
          <p role="alert" className="text-caption text-severity-critical">
            Trợ lý AI chưa trả lời được. Câu hỏi vẫn được lưu; bạn có thể hỏi lại.
          </p>
        )}
        <textarea
          id={inputId}
          rows={2}
          maxLength={2000}
          disabled={!canAsk || sending}
          value={question}
          onChange={(e) => setQuestion(e.target.value)}
          onKeyDown={onKeyDown}
          placeholder="Hỏi về hợp đồng này"
          className="w-full resize-none rounded-md border border-rule-strong bg-sheet px-3 py-2 text-body text-ink placeholder:text-ink-muted focus:border-review-ink focus:ring-3 focus:ring-review-ink/15 focus:outline-none disabled:bg-desk disabled:text-ink-muted"
        />
        <Button type="submit" size="sm" disabled={!canAsk || !question.trim()} loading={sending} className="self-end">
          Gửi
        </Button>
      </form>
    </div>
  );
}
