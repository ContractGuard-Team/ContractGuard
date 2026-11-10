import { useEffect, useRef, useState, type FormEvent } from "react";
import { Document, Page, pdfjs } from "react-pdf";
import "react-pdf/dist/Page/TextLayer.css";

import { Button } from "./Button";
import { severityLabels, type Severity } from "./SeverityTag";
import { ViewerFailed, ViewerSkeleton } from "./ViewerParts";

// A clause rectangle to wash on the page (F-VIEW-002), in fractions of the page size.
export interface PdfHighlight {
  clauseKey: string;
  page: number;
  x: number;
  y: number;
  width: number;
  height: number;
  severity: Severity;
  label: string;
}

const washClass: Record<Severity, string> = {
  critical: "bg-severity-critical/20 hover:bg-severity-critical/30 border-severity-critical",
  high: "bg-severity-high/20 hover:bg-severity-high/30 border-severity-high",
  medium: "bg-severity-medium-mark/25 hover:bg-severity-medium-mark/35 border-severity-medium-mark",
  low: "bg-severity-low/20 hover:bg-severity-low/30 border-severity-low",
};

pdfjs.GlobalWorkerOptions.workerSrc = new URL("pdfjs-dist/build/pdf.worker.min.mjs", import.meta.url).toString();

// A stable object: react-pdf reloads the document when this changes. Paths from vite.config.ts.
const pdfOptions = { cMapUrl: "/cmaps/", standardFontDataUrl: "/standard_fonts/", wasmUrl: "/wasm/", isEvalSupported: false };

const sheetWidth = 680; // DESIGN §5 Contract Sheet
const minZoom = 0.5;
const maxZoom = 2;
const zoomStep = 0.25;

// One page at a time. No annotation layer: links and forms inside an uploaded PDF stay inactive.
// Clauses with findings are washed in their severity; clicking one opens its finding.
export default function PdfViewer({
  url,
  highlights = [],
  onSelectClause,
}: {
  url: string;
  highlights?: PdfHighlight[];
  onSelectClause?: (clauseKey: string) => void;
}) {
  const [pageCount, setPageCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageDraft, setPageDraft] = useState("1");
  const [zoom, setZoom] = useState(1);
  const pageArea = useRef<HTMLDivElement>(null);
  const [areaWidth, setAreaWidth] = useState(sheetWidth);

  // The page shrinks with the area when the side panel is dragged wider or the screen is narrow.
  useEffect(() => {
    const observer = new ResizeObserver(([entry]) => {
      if (entry && entry.contentRect.width > 0) {
        setAreaWidth(entry.contentRect.width);
      }
    });
    if (pageArea.current) {
      observer.observe(pageArea.current);
    }

    return () => observer.disconnect();
  }, []);

  function goTo(target: number) {
    const next = Number.isInteger(target) ? Math.min(Math.max(target, 1), pageCount) : page;
    setPage(next);
    setPageDraft(String(next));
  }

  function onSubmitPage(event: FormEvent) {
    event.preventDefault();
    goTo(Number(pageDraft));
  }

  return (
    <div className="flex flex-col gap-4">
      {pageCount > 0 && (
        <div role="toolbar" aria-label="Điều khiển xem PDF" className="sticky top-0 z-10 flex flex-wrap items-center gap-x-4 gap-y-2 border-b border-rule bg-desk py-2">
          <div className="flex items-center gap-2">
            <Button variant="secondary" size="sm" disabled={page <= 1} onClick={() => goTo(page - 1)}>
              Trang trước
            </Button>
            <form onSubmit={onSubmitPage} className="flex items-center gap-1.5 text-label text-ink-secondary">
              <input
                aria-label="Số trang"
                inputMode="numeric"
                value={pageDraft}
                onChange={(e) => setPageDraft(e.target.value)}
                onBlur={() => goTo(Number(pageDraft))}
                className="h-9 w-12 rounded-md border border-rule-strong bg-sheet text-center text-ink focus:border-review-ink focus:ring-3 focus:ring-review-ink/15 focus:outline-none"
              />
              <span>/ {pageCount}</span>
            </form>
            <Button variant="secondary" size="sm" disabled={page >= pageCount} onClick={() => goTo(page + 1)}>
              Trang sau
            </Button>
          </div>
          <div className="flex items-center gap-1">
            <Button variant="ghost" size="sm" aria-label="Thu nhỏ" disabled={zoom <= minZoom} onClick={() => setZoom(zoom - zoomStep)}>
              −
            </Button>
            <span className="w-12 text-center text-label text-ink-secondary" aria-live="polite">
              {zoom * 100}%
            </span>
            <Button variant="ghost" size="sm" aria-label="Phóng to" disabled={zoom >= maxZoom} onClick={() => setZoom(zoom + zoomStep)}>
              +
            </Button>
          </div>
        </div>
      )}

      <div ref={pageArea} className="overflow-x-auto pb-2">
        <Document
          file={url}
          options={pdfOptions}
          suspense={false}
          loading={<ViewerSkeleton />}
          error={<ViewerFailed />}
          onLoadSuccess={({ numPages }) => setPageCount(numPages)}
        >
          <div className="relative mx-auto w-fit overflow-hidden rounded-sheet bg-sheet shadow-sheet">
            <Page pageNumber={page} width={Math.min(sheetWidth, areaWidth)} scale={zoom} renderAnnotationLayer={false} loading={<ViewerSkeleton />} error={<ViewerFailed />} />
            {highlights
              .filter((h) => h.page === page)
              .map((h, index) => (
                <button
                  key={`${h.clauseKey}-${index}`}
                  type="button"
                  title={`${severityLabels[h.severity]}: ${h.label}`}
                  aria-label={`${severityLabels[h.severity]}: ${h.label}`}
                  onClick={() => onSelectClause?.(h.clauseKey)}
                  className={`absolute z-[1] rounded-sm border-b-2 transition-colors duration-150 ${washClass[h.severity]}`}
                  style={{ left: `${h.x * 100}%`, top: `${h.y * 100}%`, width: `${h.width * 100}%`, height: `${h.height * 100}%` }}
                />
              ))}
          </div>
        </Document>
      </div>
    </div>
  );
}
