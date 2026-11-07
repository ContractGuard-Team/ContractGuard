import { useId } from "react";

import { Button } from "./Button";

// UI-GEN-002: the chat beside the contract. F-CHAT-001 comes in Phase 4; until then it stays locked and
// says so, with no made-up answers. Sans-serif throughout: this is the system's voice (DESIGN §3).
export function ChatPanel() {
  const inputId = useId();

  return (
    <div className="flex min-h-0 flex-1 flex-col">
      <div role="log" aria-label="Hội thoại với trợ lý AI" className="min-h-0 flex-1 overflow-y-auto p-4">
        <p className="text-ink-secondary">Trợ lý AI chưa được kết nối. Khi sẵn sàng, bạn có thể hỏi về hợp đồng đang mở tại đây và nhận câu trả lời kèm trích dẫn điều khoản.</p>
      </div>
      <form className="flex flex-col gap-2 border-t border-rule p-3" onSubmit={(event) => event.preventDefault()}>
        <label htmlFor={inputId} className="sr-only">
          Câu hỏi về hợp đồng
        </label>
        <textarea
          id={inputId}
          rows={2}
          disabled
          placeholder="Hỏi về hợp đồng này"
          className="w-full resize-none rounded-md border border-rule-strong bg-desk px-3 py-2 text-body text-ink-muted placeholder:text-ink-muted"
        />
        <Button type="submit" size="sm" disabled className="self-end">
          Gửi
        </Button>
      </form>
    </div>
  );
}
