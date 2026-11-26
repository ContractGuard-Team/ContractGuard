import { useEffect, useRef, useState } from "react";

import { apiDownload, saveBlob } from "~/lib/api";
import { useAction } from "~/lib/useAction";

import { Button } from "./Button";

type Format = "pdf" | "docx";

// F-EXP-001, F-EXP-002: export the report of the shown analysis. Options sit in a small popover
// (z 30) under the button; nothing leaves the page, and a failure says so (FR-EXP-004).
export function ReportExport({ jobId }: { jobId: string }) {
  const [open, setOpen] = useState(false);
  const [language, setLanguage] = useState<"vi" | "en">("vi");
  const [scope, setScope] = useState<"all" | "severe">("all");
  const [format, setFormat] = useState<Format | null>(null);
  const exporter = useAction();
  const root = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) {
      return;
    }

    const close = (event: MouseEvent | KeyboardEvent) => {
      if (event instanceof KeyboardEvent ? event.key === "Escape" : !root.current?.contains(event.target as Node)) {
        setOpen(false);
      }
    };
    document.addEventListener("mousedown", close);
    document.addEventListener("keydown", close);
    return () => {
      document.removeEventListener("mousedown", close);
      document.removeEventListener("keydown", close);
    };
  }, [open]);

  function download(target: Format) {
    setFormat(target);
    void exporter.run(async () => {
      const { blob, fileName } = await apiDownload(`/jobs/${jobId}/report?format=${target}&language=${language}&scope=${scope}`);
      saveBlob(blob, fileName);
      setOpen(false);
    });
  }

  const selectClass = "h-9 w-full rounded-md border border-rule-strong bg-sheet px-2 text-body focus:border-review-ink focus:ring-3 focus:ring-review-ink/15 focus:outline-none";

  return (
    <div ref={root} className="relative">
      <Button variant="secondary" size="sm" aria-expanded={open} aria-controls="report-export" onClick={() => setOpen(!open)}>
        Xuất báo cáo
      </Button>
      {exporter.error !== null && !open && (
        <p role="alert" className="absolute top-full right-0 z-30 mt-1 w-64 rounded-md border border-severity-critical/30 bg-severity-critical-wash px-3 py-2 text-caption">
          Tạo báo cáo thất bại. Vui lòng thử lại sau ít phút.
        </p>
      )}
      {open && (
        <div id="report-export" className="absolute top-full right-0 z-30 mt-1 flex w-64 flex-col gap-3 rounded-md border border-rule bg-sheet p-3">
          <label className="flex flex-col gap-1 text-label">
            Ngôn ngữ báo cáo
            <select value={language} onChange={(e) => setLanguage(e.target.value as "vi" | "en")} className={selectClass}>
              <option value="vi">Tiếng Việt</option>
              <option value="en">English</option>
            </select>
            {/* docs/06 §3.3: only the report's own wording is translated. */}
            <span className="text-caption font-normal text-ink-secondary">Nội dung finding giữ ngôn ngữ lúc phân tích.</span>
          </label>
          <label className="flex flex-col gap-1 text-label">
            Phạm vi rủi ro
            <select value={scope} onChange={(e) => setScope(e.target.value as "all" | "severe")} className={selectClass}>
              <option value="all">Tất cả finding</option>
              <option value="severe">Chỉ mức Cao và Nghiêm trọng</option>
            </select>
          </label>
          {exporter.error !== null && (
            <p role="alert" className="text-caption text-severity-critical">
              Tạo báo cáo thất bại. Vui lòng thử lại.
            </p>
          )}
          <div className="flex gap-2">
            <Button size="sm" className="flex-1" loading={exporter.pending && format === "pdf"} disabled={exporter.pending} onClick={() => download("pdf")}>
              PDF
            </Button>
            <Button variant="secondary" size="sm" className="flex-1" loading={exporter.pending && format === "docx"} disabled={exporter.pending} onClick={() => download("docx")}>
              DOCX
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
